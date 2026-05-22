// Файл: Services/ApiService.cs
// Все HTTP-запросы к бэкенду собраны здесь.
// Авторизация: Bearer JWT в заголовке Authorization.
// Сериализация: System.Text.Json (встроен в .NET, без лишних пакетов).

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using YandexMessengerMAUI.Models;

namespace YandexMessengerMAUI.Services;

public class ApiService
{
    private readonly HttpClient _http;

    // Настройки десериализации: игнорируем регистр имён свойств
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConstants.ServerBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    // ─── Авторизация ──────────────────────────────────────────────────────────

    /// <summary>
    /// Получить ссылку для входа через Яндекс OAuth.
    /// GET /api/auth/login → { url: "https://oauth.yandex.ru/..." }
    /// </summary>
    public async Task<string?> GetLoginUrlAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/auth/login");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("url").GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] GetLoginUrl error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Установить юзернейм для нового пользователя.
    /// POST /api/auth/set-username
    /// </summary>
    public async Task<(AuthResponse? result, string? error)> SetUsernameAsync(string username)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync(
                "/api/auth/set-username",
                new SetUsernameRequest { Username = username },
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                return (result, null);
            }

            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions);
            return (null, err?.Error ?? "Неизвестная ошибка");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Обработать результат OAuth-редиректа.
    /// Приложение перехватывает deep link вида: yandexmessenger://callback?token=...
    /// и передаёт сюда токен из query string.
    /// GET /api/auth/me — проверяем, что токен валиден.
    /// </summary>
    public async Task<(UserProfile? profile, string? error)> GetMyProfileAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync("/api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(_jsonOptions);
                return (profile, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return (null, "Необходима авторизация");

            return (null, "Ошибка сервера");
        }
        catch (HttpRequestException)
        {
            return (null, "Нет соединения с сервером");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // ─── Сообщения ────────────────────────────────────────────────────────────

    /// <summary>
    /// Отправить сообщение.
    /// POST /api/messages
    /// Body: { recipientUsername, text }
    /// </summary>
    public async Task<(ChatMessage? message, string? error)> SendMessageAsync(
        string recipientUsername, string text)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.PostAsJsonAsync(
                "/api/messages",
                new SendMessageRequest { RecipientUsername = recipientUsername, Text = text },
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadFromJsonAsync<ChatMessage>(_jsonOptions);
                return (msg, null);
            }

            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions);
            return (null, err?.Error ?? "Ошибка отправки");
        }
        catch (HttpRequestException)
        {
            return (null, "Нет соединения с сервером");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Загрузить историю чата с пользователем.
    /// GET /api/messages/history?withUsername=alice&page=1&pageSize=50
    /// </summary>
    public async Task<(List<ChatMessage>? messages, string? error)> GetHistoryAsync(
        string withUsername, int page = 1, int pageSize = 50)
    {
        SetAuthHeader();
        try
        {
            var url = $"/api/messages/history?withUsername={Uri.EscapeDataString(withUsername)}" +
                      $"&page={page}&pageSize={pageSize}";

            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var messages = await response.Content
                    .ReadFromJsonAsync<List<ChatMessage>>(_jsonOptions);
                return (messages ?? new(), null);
            }

            return (null, "Не удалось загрузить историю");
        }
        catch (HttpRequestException)
        {
            return (null, "Нет соединения с сервером");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Загрузить список диалогов (экран «Чаты»).
    /// GET /api/messages/dialogs
    /// </summary>
    public async Task<(List<Dialog>? dialogs, string? error)> GetDialogsAsync()
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync("/api/messages/dialogs");
            if (response.IsSuccessStatusCode)
            {
                var dialogs = await response.Content
                    .ReadFromJsonAsync<List<Dialog>>(_jsonOptions);
                return (dialogs ?? new(), null);
            }

            return (null, "Не удалось загрузить чаты");
        }
        catch (HttpRequestException)
        {
            return (null, "Нет соединения с сервером");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Найти пользователя по юзернейму.
    /// GET /api/messages/find-user?username=alice
    /// </summary>
    public async Task<(UserInfo? user, string? error)> FindUserAsync(string username)
    {
        SetAuthHeader();
        try
        {
            var response = await _http.GetAsync(
                $"/api/messages/find-user?username={Uri.EscapeDataString(username)}");

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions);
                return (user, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return (null, $"Пользователь @{username} не найден");

            return (null, "Ошибка поиска");
        }
        catch (HttpRequestException)
        {
            return (null, "Нет соединения с сервером");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // ─── Вспомогательное ─────────────────────────────────────────────────────

    /// <summary>Установить JWT токен в заголовок Authorization: Bearer ...</summary>
    public void SetJwtToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Очистить токен (выход из аккаунта)</summary>
    public void ClearAuth()
    {
        _http.DefaultRequestHeaders.Authorization = null;
    }

    private void SetAuthHeader()
    {
        // Токен уже установлен через SetJwtToken() при входе.
        // Этот метод — страховка: повторно читаем из SecureStorage если нужно.
        if (_http.DefaultRequestHeaders.Authorization != null) return;

        var token = SecureStorage.Default.GetAsync(AppConstants.JwtTokenKey).Result;
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }
}
