// Файл: ViewModels/SetUsernameViewModel.cs
// ViewModel для экрана выбора юзернейма (только при первом входе).

using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.ViewModels;

public class SetUsernameViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public event Action? UsernameSet;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            ClearError();
            OnPropertyChanged(nameof(IsUsernameValid));
        }
    }

    public bool IsUsernameValid =>
        Username.Length >= 3 &&
        Username.Length <= 20 &&
        Username.All(c => char.IsLetterOrDigit(c) || c == '_');

    public SetUsernameViewModel(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
        ConfirmCommand = new AsyncRelayCommand(OnConfirmAsync, () => IsUsernameValid && IsNotBusy);
    }

    public AsyncRelayCommand ConfirmCommand { get; }

    private async Task OnConfirmAsync()
    {
        if (!IsUsernameValid) return;

        IsBusy = true;
        ClearError();
        try
        {
            var (result, error) = await _api.SetUsernameAsync(Username.Trim().ToLower());
            if (error != null)
            {
                SetError(error);
                return;
            }

            if (result == null)
            {
                SetError("Неизвестная ошибка. Попробуйте снова.");
                return;
            }

            await _auth.SaveSessionAsync(result.Token, result.Username, result.YandexEmail);
            UsernameSet?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
