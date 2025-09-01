using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace AdminP.Services;

public class NotificationService : INotificationService, IAsyncDisposable
{
    private readonly List<Notification> _notifications = new();
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;
    private readonly string _hubUrl;

    public event Action? OnNotificationsChanged;
    public event Action<Guid>? OnAddComputerRequested;
    public IReadOnlyList<Notification> Notifications => _notifications;

    public NotificationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _hubUrl = _configuration["SignalR:HubUrl"] ?? "http://192.168.1.87:5100/api/admins/hub";

        _ = InitializeHubAsync();
    }


    private async Task InitializeHubAsync()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Handle session requests
            _hubConnection.On<SessionRequest>("SessionRequested", request =>
            {
                AddNotification(new Notification
                {
                    Type = NotificationType.SessionRequest,
                    Title = "Запрос на сессию",
                    Message = $"Пользователь {request.Username} хочет начать сессию на ПК {request.PcId}",
                    IsPersistent = true,
                    Data = request
                });
            });

            // Handle unknown PC metrics -> propose creating computer
            _hubConnection.On<UnknownPcData>("UnknownPcDetected", data =>
            {
                AddNotification(new Notification
                {
                    Type = NotificationType.UnknownPc,
                    Title = "Неизвестный ПК",
                    Message = $"ПК с UUID {data.Uuid} отправил метрики, но не найден в базе",
                    IsPersistent = true,
                    Data = data
                });
                OnAddComputerRequested?.Invoke(data.Uuid);
            });

            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            // Add error notification if hub connection fails
            AddNotification(new Notification
            {
                Type = NotificationType.Error,
                Title = "Ошибка подключения",
                Message = "Не удалось подключиться к серверу уведомлений",
                IsPersistent = false
            });
        }
    }

    public void AddNotification(Notification notification)
    {
        _notifications.Add(notification);
        OnNotificationsChanged?.Invoke();
    }

    public void RemoveNotification(string id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _notifications.Remove(notification);
            OnNotificationsChanged?.Invoke();
        }
    }

    public void ClearAll()
    {
        _notifications.Clear();
        OnNotificationsChanged?.Invoke();
    }

    public async Task SendSessionResponseAsync(SessionResponse response)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            // Send full DTO back to backend
            await _hubConnection.InvokeAsync("SendSessionResponse", response);
        }
    }

    public void RequestAddComputer(Guid uuid)
    {
        OnAddComputerRequested?.Invoke(uuid);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

// DTOs for SignalR communication
public class SessionRequest
{
    public int PcId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class SessionResponse
{
    public int PcId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public bool IsApproved { get; set; } = true;
}

public class UnknownPcData
{
    public Guid Uuid { get; set; }
}
