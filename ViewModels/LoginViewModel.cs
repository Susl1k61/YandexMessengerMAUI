// Файл: ViewModels/LoginViewModel.cs
// ViewModel для экрана входа.
// Открывает браузер для OAuth → получает deep link с токеном.

using YandexMessengerMAUI.Models;
using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    // Срабатывает когда вход успешен — навигация происходит на уровне Page
    public event Action? LoginSucceeded;
    // Срабатывает если нужно выбрать юзернейм (новый пользователь)
    public event Action<string>? NeedsUsername; // передаём временный JWT

    public LoginViewModel(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
        LoginCommand = new AsyncRelayCommand(OnLoginAsync);
    }

    public AsyncRelayCommand LoginCommand { get; }

    /// <summary>
    /// Нажали «Войти через Яндекс»:
    /// 1. Получаем OAuth URL с сервера
    /// 2. Открываем в системном браузере
    /// 3. После авторизации Яндекс редиректит на deep link:
    ///    yandexmessenger://callback?token=JWT&username=alice&needs_username=false
    /// 4. App.xaml.cs перехватывает deep link и вызывает HandleOAuthCallbackAsync()
    /// </summary>
    private async Task OnLoginAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var url = await _api.GetLoginUrlAsync();
            if (string.IsNullOrEmpty(url))
            {
                SetError("Не удалось подключиться к серверу.\nПроверьте, что сервер запущен.");
                return;
            }

            // Открываем браузер с OAuth страницей Яндекса
            await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
            // Дальше ждём deep link в App.xaml.cs → OnAppLinkRequestReceived
        }
        catch (Exception ex)
        {
            SetError($"Ошибка: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Вызывается из App.xaml.cs когда приложение получило deep link от Яндекса.
    /// Формат ссылки (настраивается на бэкенде):
    ///   yandexmessenger://callback?token=JWT&username=alice&needs_username=false
    /// </summary>
    public async Task HandleOAuthCallbackAsync(Uri callbackUri)
    {
        IsBusy = true;
        ClearError();
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(callbackUri.Query);
            var token = query["token"];
            var username = query["username"] ?? string.Empty;
            var needsUsername = query["needs_username"] == "true";
            var errorParam = query["error"];

            if (!string.IsNullOrEmpty(errorParam))
            {
                SetError($"Ошибка авторизации: {errorParam}");
                return;
            }

            if (string.IsNullOrEmpty(token))
            {
                SetError("Токен не получен. Попробуйте снова.");
                return;
            }

            if (needsUsername)
            {
                // Новый пользователь — нужно выбрать юзернейм
                // Временно сохраняем токен для вызова /set-username
                _api.SetJwtToken(token);
                NeedsUsername?.Invoke(token);
                return;
            }

            // Успешный вход — сохраняем сессию
            var email = query["email"] ?? string.Empty;
            await _auth.SaveSessionAsync(token, username, email);
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            SetError($"Ошибка обработки ответа: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
