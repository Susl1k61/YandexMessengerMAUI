// Файл: ViewModels/NewChatViewModel.cs
// ViewModel для экрана «Новый чат» (поиск пользователя по юзернейму).

using YandexMessengerMAUI.Models;
using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.ViewModels;

public class NewChatViewModel : BaseViewModel
{
    private readonly ApiService _api;

    public event Action<string>? UserFound; // передаём username найденного пользователя

    private string _searchUsername = string.Empty;
    public string SearchUsername
    {
        get => _searchUsername;
        set
        {
            SetProperty(ref _searchUsername, value);
            ClearError();
            FoundUser = null;
        }
    }

    private UserInfo? _foundUser;
    public UserInfo? FoundUser
    {
        get => _foundUser;
        set
        {
            SetProperty(ref _foundUser, value);
            OnPropertyChanged(nameof(HasFoundUser));
        }
    }
    public bool HasFoundUser => FoundUser != null;

    public NewChatViewModel(ApiService api)
    {
        _api = api;
        SearchCommand = new AsyncRelayCommand(OnSearchAsync, () => !string.IsNullOrWhiteSpace(SearchUsername));
        OpenChatCommand = new RelayCommand(OnOpenChat, () => HasFoundUser);
    }

    public AsyncRelayCommand SearchCommand { get; }
    public RelayCommand OpenChatCommand { get; }

    private async Task OnSearchAsync()
    {
        IsBusy = true;
        ClearError();
        FoundUser = null;
        try
        {
            var username = SearchUsername.Trim().TrimStart('@').ToLower();
            var (user, error) = await _api.FindUserAsync(username);
            if (error != null) { SetError(error); return; }
            FoundUser = user;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnOpenChat()
    {
        if (FoundUser != null)
            UserFound?.Invoke(FoundUser.Username);
    }
}
