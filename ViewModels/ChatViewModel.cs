// Файл: ViewModels/ChatViewModel.cs
// ViewModel для экрана переписки с конкретным пользователем.

using System.Collections.ObjectModel;
using YandexMessengerMAUI.Models;
using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly SignalRService _signalR;

    // Сообщения в чате — привязаны к CollectionView
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    // Событие — прокрутить список вниз (вызывается из Page)
    public event Action? ScrollToBottom;

    private string _recipientUsername = string.Empty;
    public string RecipientUsername
    {
        get => _recipientUsername;
        set => SetProperty(ref _recipientUsername, value);
    }

    private string _recipientDisplayName = string.Empty;
    public string RecipientDisplayName
    {
        get => _recipientDisplayName;
        set => SetProperty(ref _recipientDisplayName, value);
    }

    private bool _isRecipientOnline;
    public bool IsRecipientOnline
    {
        get => _isRecipientOnline;
        set
        {
            SetProperty(ref _isRecipientOnline, value);
            OnPropertyChanged(nameof(RecipientStatusText));
        }
    }
    public string RecipientStatusText => IsRecipientOnline ? "онлайн" : "не в сети";

    private string _messageText = string.Empty;
    public string MessageText
    {
        get => _messageText;
        set
        {
            SetProperty(ref _messageText, value);
            OnPropertyChanged(nameof(CanSend));
        }
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && IsNotBusy;

    public ChatViewModel(ApiService api, AuthService auth, SignalRService signalR)
    {
        _api = api;
        _auth = auth;
        _signalR = signalR;

        SendCommand = new AsyncRelayCommand(OnSendAsync, () => CanSend);
        LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);

        _signalR.MessageReceived += OnMessageReceived;
    }

    public AsyncRelayCommand SendCommand { get; }
    public AsyncRelayCommand LoadMoreCommand { get; }

    /// <summary>Инициализация чата: загрузить историю, получить профиль собеседника</summary>
    public async Task InitializeAsync(string recipientUsername)
    {
        RecipientUsername = recipientUsername;
        RecipientDisplayName = $"@{recipientUsername}";

        // Загружаем информацию о собеседнике
        var (userInfo, _) = await _api.FindUserAsync(recipientUsername);
        if (userInfo != null)
        {
            RecipientDisplayName = userInfo.DisplayTitle;
            IsRecipientOnline = userInfo.IsOnline;
        }

        await LoadHistoryAsync();

        // Помечаем сообщения как прочитанные
        await _signalR.MarkAsReadAsync(recipientUsername);
    }

    private int _currentPage = 1;
    private bool _hasMore = true;

    private async Task LoadHistoryAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            _currentPage = 1;
            var (messages, error) = await _api.GetHistoryAsync(RecipientUsername, 1);
            if (error != null) { SetError(error); return; }

            Messages.Clear();
            var myUsername = _auth.CurrentUsername ?? string.Empty;
            foreach (var m in messages ?? new())
            {
                m.IsMine = m.SenderUsername == myUsername;
                Messages.Add(m);
            }

            _hasMore = (messages?.Count ?? 0) >= 50;
            ScrollToBottom?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Подгрузить более старые сообщения (paging)</summary>
    private async Task LoadMoreAsync()
    {
        if (!_hasMore || IsBusy) return;

        IsBusy = true;
        try
        {
            _currentPage++;
            var (messages, _) = await _api.GetHistoryAsync(RecipientUsername, _currentPage);
            if (messages == null || messages.Count == 0)
            {
                _hasMore = false;
                return;
            }

            var myUsername = _auth.CurrentUsername ?? string.Empty;
            // Добавляем старые сообщения в начало списка
            for (int i = 0; i < messages.Count; i++)
            {
                messages[i].IsMine = messages[i].SenderUsername == myUsername;
                Messages.Insert(i, messages[i]);
            }

            _hasMore = messages.Count >= 50;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnSendAsync()
    {
        var text = MessageText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        MessageText = string.Empty;  // сразу очищаем поле

        var (message, error) = await _api.SendMessageAsync(RecipientUsername, text);
        if (error != null)
        {
            SetError(error);
            MessageText = text;  // возвращаем текст если ошибка
            return;
        }

        if (message != null)
        {
            message.IsMine = true;
            Messages.Add(message);
            ScrollToBottom?.Invoke();
        }
    }

    /// <summary>Входящее сообщение через SignalR — добавляем в список если это наш чат</summary>
    private void OnMessageReceived(ChatMessage message)
    {
        // Показываем только сообщения из текущего открытого чата
        if (message.SenderUsername != RecipientUsername &&
            message.RecipientUsername != RecipientUsername)
            return;

        var myUsername = _auth.CurrentUsername ?? string.Empty;
        message.IsMine = message.SenderUsername == myUsername;
        Messages.Add(message);
        ScrollToBottom?.Invoke();

        // Сразу помечаем прочитанным
        _ = _signalR.MarkAsReadAsync(RecipientUsername);
    }

    public void Cleanup()
    {
        _signalR.MessageReceived -= OnMessageReceived;
    }
}
