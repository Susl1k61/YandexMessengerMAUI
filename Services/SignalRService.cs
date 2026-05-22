// Файл: Services/SignalRService.cs
// Управляет WebSocket-соединением с SignalR хабом на сервере.
// Получает новые сообщения и уведомления о прочтении в реальном времени.

using Microsoft.AspNetCore.SignalR.Client;
using YandexMessengerMAUI.Models;

namespace YandexMessengerMAUI.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;

    // ─── События — ViewModel подписывается на них ─────────────────────────────

    /// <summary>Срабатывает когда пришло новое сообщение</summary>
    public event Action<ChatMessage>? MessageReceived;

    /// <summary>Срабатывает когда собеседник прочитал наши сообщения</summary>
    public event Action<string>? MessagesReadByPeer;

    /// <summary>Срабатывает при изменении статуса соединения</summary>
    public event Action<string>? ConnectionStatusChanged;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Подключиться к SignalR хабу.
    /// JWT передаётся в query string (?access_token=...) — стандарт для SignalR.
    /// </summary>
    public async Task ConnectAsync(string jwtToken)
    {
        if (_connection != null)
            await DisconnectAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl(AppConstants.SignalRHubUrl, options =>
            {
                // SignalR через WebSocket передаёт JWT в query string
                options.AccessTokenProvider = () => Task.FromResult<string?>(jwtToken);
            })
            .WithAutomaticReconnect(new[] { // стратегия переподключения (секунды)
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
            })
            .Build();

        // ─── Подписка на события от сервера ────────────────────────────────

        // Сервер вызывает этот метод при каждом новом входящем сообщении
        _connection.On<ChatMessage>("ReceiveMessage", message =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                MessageReceived?.Invoke(message));
        });

        // Сервер уведомляет, что собеседник прочитал наши сообщения
        _connection.On<object>("MessagesRead", data =>
        {
            // data: { by: "username", at: "2024-..." }
            var by = data?.ToString() ?? string.Empty;
            MainThread.BeginInvokeOnMainThread(() =>
                MessagesReadByPeer?.Invoke(by));
        });

        // ─── События состояния соединения ──────────────────────────────────

        _connection.Reconnecting += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusChanged?.Invoke("Переподключение..."));
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusChanged?.Invoke("Подключено"));
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusChanged?.Invoke("Отключено"));
            return Task.CompletedTask;
        };

        // ─── Запуск соединения ─────────────────────────────────────────────
        try
        {
            await _connection.StartAsync();
            ConnectionStatusChanged?.Invoke("Подключено");
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke($"Ошибка: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Сообщить серверу, что мы прочитали сообщения от указанного пользователя.
    /// Сервер уведомит этого пользователя через его SignalR-канал.
    /// </summary>
    public async Task MarkAsReadAsync(string senderUsername)
    {
        if (!IsConnected || _connection == null) return;
        try
        {
            await _connection.InvokeAsync("MarkAsRead", senderUsername);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] MarkAsRead error: {ex.Message}");
        }
    }

    /// <summary>Проверить соединение (Ping → Pong)</summary>
    public async Task PingAsync()
    {
        if (!IsConnected || _connection == null) return;
        try
        {
            await _connection.InvokeAsync("Ping");
        }
        catch { /* игнорируем */ }
    }

    public async Task DisconnectAsync()
    {
        if (_connection == null) return;
        try
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
        catch { /* игнорируем ошибки при отключении */ }
        finally
        {
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
