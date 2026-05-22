// Файл: Models/Models.cs
// Все модели данных, которые приходят от сервера или отправляются на него.
namespace YandexMessengerMAUI.Models;

/// <summary>Ответ сервера после успешной авторизации через Яндекс</summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string YandexEmail { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    /// <summary>true — новый пользователь, нужно выбрать юзернейм</summary>
    public bool NeedsUsername { get; set; }
}

/// <summary>Профиль текущего пользователя (GET /api/auth/me)</summary>
public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string YandexEmail { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
}

/// <summary>Краткая информация о другом пользователе</summary>
public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }

    public string DisplayTitle => DisplayName ?? $"@{Username}";
    public string OnlineStatus => IsOnline ? "онлайн" : "не в сети";
}

/// <summary>Сообщение в чате</summary>
public class ChatMessage
{
    public int Id { get; set; }
    public string MessageGuid { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string RecipientUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Вычисляется на клиенте: true если это моё сообщение</summary>
    public bool IsMine { get; set; }

    public string TimeLabel => SentAt.ToLocalTime().ToString("HH:mm");
    public bool IsRead => ReadAt.HasValue;
}

/// <summary>Диалог в списке чатов</summary>
public class Dialog
{
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string LastMessageText { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }

    public string DisplayTitle => DisplayName ?? $"@{Username}";
    public string LastMessagePreview =>
        LastMessageText.Length > 40
            ? LastMessageText[..40] + "…"
            : LastMessageText;
    public string TimeLabel
    {
        get
        {
            var local = LastMessageAt.ToLocalTime();
            return local.Date == DateTime.Today
                ? local.ToString("HH:mm")
                : local.ToString("dd.MM");
        }
    }
    public bool HasUnread => UnreadCount > 0;
}

/// <summary>Запрос на отправку сообщения (POST /api/messages)</summary>
public class SendMessageRequest
{
    public string RecipientUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>Запрос на установку юзернейма (POST /api/auth/set-username)</summary>
public class SetUsernameRequest
{
    public string Username { get; set; } = string.Empty;
}

/// <summary>Ответ сервера при ошибке</summary>
public class ErrorResponse
{
    public string? Error { get; set; }
}
