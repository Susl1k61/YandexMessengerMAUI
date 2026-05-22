// Файл: Services/AuthService.cs
// Управляет сессией пользователя: хранение JWT, вход/выход.
// JWT хранится в SecureStorage (зашифрованное хранилище ОС).

namespace YandexMessengerMAUI.Services;

public class AuthService
{
    private readonly ApiService _api;

    public string? CurrentJwt { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentEmail { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentJwt);

    public AuthService(ApiService api)
    {
        _api = api;
    }

    /// <summary>
    /// Попытаться восстановить сессию из SecureStorage при запуске приложения.
    /// Возвращает true если сессия валидна.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(AppConstants.JwtTokenKey);
            if (string.IsNullOrEmpty(token)) return false;

            // Проверяем токен на сервере
            _api.SetJwtToken(token);
            var (profile, error) = await _api.GetMyProfileAsync();
            if (profile == null)
            {
                await ClearSessionAsync();
                return false;
            }

            CurrentJwt = token;
            CurrentUsername = profile.Username;
            CurrentEmail = profile.YandexEmail;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Сохранить сессию после успешного входа</summary>
    public async Task SaveSessionAsync(string jwt, string username, string email)
    {
        CurrentJwt = jwt;
        CurrentUsername = username;
        CurrentEmail = email;

        _api.SetJwtToken(jwt);

        await SecureStorage.Default.SetAsync(AppConstants.JwtTokenKey, jwt);
        await SecureStorage.Default.SetAsync(AppConstants.UsernameKey, username);
        await SecureStorage.Default.SetAsync(AppConstants.YandexEmailKey, email);
    }

    /// <summary>Выход: очищаем токен из памяти и хранилища</summary>
    public async Task ClearSessionAsync()
    {
        CurrentJwt = null;
        CurrentUsername = null;
        CurrentEmail = null;

        _api.ClearAuth();

        SecureStorage.Default.Remove(AppConstants.JwtTokenKey);
        SecureStorage.Default.Remove(AppConstants.UsernameKey);
        SecureStorage.Default.Remove(AppConstants.YandexEmailKey);

        await Task.CompletedTask;
    }
}
