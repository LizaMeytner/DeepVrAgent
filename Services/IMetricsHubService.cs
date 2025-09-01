namespace AdminP.Services;

public interface IMetricsHubService : IAsyncDisposable
{
    Task StartAsync(string hubUrl);
    
    Task SendDeletePcAsync(Guid uuid);
    
    Task OnPcAdded(Guid uuid, string name, string hostName, string ip);
    
    // Events for hub communication
    event Action<Guid>? OnAddPcRequested;
    event Action<Guid>? OnPcDeleted;
    event Action<Guid>? OnPcAddedEvent;
}


