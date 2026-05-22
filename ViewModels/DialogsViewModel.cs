// Файл: ViewModels/DialogsViewModel.cs
// ViewModel для экрана «Чаты» (список диалогов).
// Загружает список чатов и слушает новые сообщения через SignalR.

using System.Collections.ObjectModel;
using YandexMessengerMAUI.Models;
using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.ViewModels;

public class DialogsViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly SignalRService _signalR;

    // Список диалогов — привязан к CollectionView в XAML
    public ObservableCollection<Dialog> Dialogs { get; } = new();

    // Событие — открыть конкретный чат
    public event Action<string>? OpenChat;
    // Событие — открыть экран поиска нового собеседника
    public event Action? OpenNewChat;
    // Событие — выход из аккаунта
    public event Action? LoggedOut;

    private string _connectionStatus = "Подключение...";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    private string _myUsername = string.Empty;
    public string MyUsername
    {
        get => _myUsername;
        set => SetProperty(ref _myUsername, value);
    }

    public DialogsViewModel(ApiService api, AuthService auth, SignalRService signalR)
    {
        _api = api;
        _auth = auth;
        _signalR = signalR;

        RefreshCommand = new AsyncRelayCommand(LoadDialogsAsync);
        NewChatCommand = new RelayCommand(() => OpenNewChat?.Invoke());
        LogoutCommand = new AsyncRelayCommand(OnLogoutAsync);

        // Подписываемся на входящие сообщения от SignalR
        _signalR.MessageReceived += OnMessageReceived;
        _signalR.ConnectionStatusChanged += status =>
            MainThread.BeginInvokeOnMainThread(() => ConnectionStatus = status);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand NewChatCommand { get; }
    public AsyncRelayCommand LogoutCommand { get; }

    /// <summary>Инициализация: подключиться к SignalR и загрузить диалоги</summary>
    public async Task InitializeAsync()
    {
        MyUsername = _auth.CurrentUsername ?? string.Empty;

        // Подключаемся к SignalR (запускает IMAP-мониторинг на сервере)
        try
        {
            await _signalR.ConnectAsync(_auth.CurrentJwt!);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Нет соединения: {ex.Message}";
        }

        await LoadDialogsAsync();
    }

    private async Task LoadDialogsAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var (dialogs, error) = await _api.GetDialogsAsync();
            if (error != null) { SetError(error); return; }

            Dialogs.Clear();
            foreach (var d in dialogs ?? new())
                Dialogs.Add(d);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Когда пришло новое сообщение через SignalR — обновляем список диалогов</summary>
    private void OnMessageReceived(ChatMessage message)
    {
        // Обновляем или добавляем диалог в список
        var senderName = message.SenderUsername == MyUsername
            ? message.RecipientUsername
            : message.SenderUsername;

        var existing = Dialogs.FirstOrDefault(d => d.Username == senderName);
        if (existing != null)
        {
            // Обновляем существующий диалог
            var updated = new Dialog
            {
                Username = existing.Username,
                DisplayName = existing.DisplayName,
                AvatarUrl = existing.AvatarUrl,
                LastMessageText = message.Text,
                LastMessageAt = message.SentAt,
                UnreadCount = message.SenderUsername != MyUsername
                    ? existing.UnreadCount + 1
                    : existing.UnreadCount,
            };
            var idx = Dialogs.IndexOf(existing);
            Dialogs[idx] = updated;
        }
        else
        {
            // Новый диалог
            Dialogs.Insert(0, new Dialog
            {
                Username = senderName,
                LastMessageText = message.Text,
                LastMessageAt = message.SentAt,
                UnreadCount = message.SenderUsername != MyUsername ? 1 : 0,
            });
        }
    }

    public void OpenChatWith(string username) => OpenChat?.Invoke(username);

    private async Task OnLogoutAsync()
    {
        await _signalR.DisconnectAsync();
        await _auth.ClearSessionAsync();
        LoggedOut?.Invoke();
    }

    public void Cleanup()
    {
        _signalR.MessageReceived -= OnMessageReceived;
    }
}
