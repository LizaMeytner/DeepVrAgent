using AdminP.Shared;

namespace AdminP.Services;

public interface INotificationService
{
    event Action? OnNotificationsChanged;
    event Action<Guid>? OnAddComputerRequested;
    IReadOnlyList<Notification> Notifications { get; }
    
    void AddNotification(Notification notification);
    void RemoveNotification(string id);
    void ClearAll();
    Task SendSessionResponseAsync(SessionResponse response);
    void RequestAddComputer(Guid uuid);
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsPersistent { get; set; }
    public object? Data { get; set; } // for session requests, unknown PC data, etc.
}

public enum NotificationType
{
    Info,
    Warning,
    Error,
    SessionRequest,
    UnknownPc
}
