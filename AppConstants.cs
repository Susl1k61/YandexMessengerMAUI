// Файл: AppConstants.cs
// Единственное место, где хранится URL сервера.
// Измените ServerBaseUrl на адрес вашего бэкенда.
namespace YandexMessengerMAUI;

public static class AppConstants
{
    /// <summary>
    /// URL бэкенда. При локальном запуске используйте:
    ///   Android-эмулятор: http://10.0.2.2:5000
    ///   Windows/Mac:      http://localhost:5000
    /// </summary>
#if ANDROID
    public const string ServerBaseUrl = "http://10.0.2.2:5000";
#else
    public const string ServerBaseUrl = "http://localhost:5000";
#endif

    public const string SignalRHubUrl = ServerBaseUrl + "/chatHub";

    // Ключи для SecureStorage
    public const string JwtTokenKey = "jwt_token";
    public const string UsernameKey = "username";
    public const string YandexEmailKey = "yandex_email";
}
