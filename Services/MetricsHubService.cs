using AdminP.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace AdminP.Services;

public class MetricsHubService : IMetricsHubService
{
    private HubConnection? _connection;
    private readonly NotificationService _notificationService;
    
    // Events for hub communication
    public event Action<Guid>? OnAddPcRequested;
    public event Action<Guid>? OnPcDeleted;
    public event Action<Guid>? OnPcAddedEvent;

    public MetricsHubService(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task StartAsync(string hubUrl)
    {
        // Ensure the hub URL ends with /hub for SignalR
        var normalizedHubUrl = hubUrl.EndsWith("/hub") ? hubUrl : $"{hubUrl}/hub";
        
        _connection = new HubConnectionBuilder()
            .WithUrl(normalizedHubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Handle PC added requests from hub
        _connection.On<Guid>("PcAdded", (uuid) => OnAddPcRequested?.Invoke(uuid));
        // _connection.On<Guid>("PcDeleted", (uuid) => OnPcDeleted?.Invoke(uuid));

        await _connection.StartAsync();
    }
    
    public async Task SendDeletePcAsync(Guid uuid)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("DeletePc", uuid);
        }
    }

    public async Task OnPcAdded(Guid uuid, string name, string hostName, string ip)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("PcAdded", new { uuid, name, hostName, ip });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}


