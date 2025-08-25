using DeepVRAgent.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DeepVRAgent.Services;

public class HubClientService : BackgroundService
{
	private readonly ILogger<HubClientService> _logger;
	private readonly IConfiguration _configuration;
	private readonly MetricsCollector _metricsCollector;
	private HubConnection? _connection;

	public HubClientService(ILogger<HubClientService> logger, IConfiguration configuration, MetricsCollector metricsCollector)
	{
		_logger = logger;
		_configuration = configuration;
		_metricsCollector = metricsCollector;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var host = _configuration["HostServer:Host"] ?? "192.168.245.193";
		var port = _configuration["HostServer:Port"] ?? "5100";
		var url = $"http://{host}:{port}/api/metrics-stream";

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				if (_connection == null)
				{
					_logger.LogInformation("Creating SignalR connection to {Url}", url);
					_connection = new HubConnectionBuilder()
						.WithUrl(url)
						.WithAutomaticReconnect()
						.Build();

					_connection.Closed += async (error) =>
					{
						_logger.LogWarning(error, "SignalR connection closed. Reconnecting in 5s...");
						await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
						// reconnect in loop
					};
				}

				if (_connection.State != HubConnectionState.Connected)
				{
					_logger.LogInformation("Starting SignalR connection...");
					await _connection.StartAsync(stoppingToken);
					_logger.LogInformation("SignalR connected to {Url}", url);
				}

				MetricsMessage metrics = _metricsCollector.GetMetrics();
				_logger.LogDebug("Sending metrics via SignalR: {Metrics}", metrics);
				var sent = false;
				try
				{
					await _connection.InvokeAsync("SendMetrics", metrics, cancellationToken: stoppingToken);
					sent = true;
				}
				catch (Exception ex1)
				{
					_logger.LogWarning(ex1, "SendMetrics with payload failed; trying without payload...");
					try
					{
						await _connection.InvokeAsync("SendMetrics", cancellationToken: stoppingToken);
						sent = true;
					}
					catch (Exception ex2)
					{
						_logger.LogError(ex2, "SendMetrics without payload also failed");
					}
				}
				if (sent)
				{
					_logger.LogDebug("SignalR invocation sent successfully");
				}

				// Дополнительно отправляем текстовое сообщение
				try
				{
					await _connection.InvokeAsync("SendMessage", "dance with me one more time", cancellationToken: stoppingToken);
					_logger.LogDebug("Sent phrase via SendMessage");
				}
				catch (Exception exMsg1)
				{
					_logger.LogWarning(exMsg1, "SendMessage failed; trying SendMetrics with string payload...");
					try
					{
						await _connection.InvokeAsync("SendMetrics", "dance with me one more time", cancellationToken: stoppingToken);
						_logger.LogDebug("Sent phrase via SendMetrics(string)");
					}
					catch (Exception exMsg2)
					{
						_logger.LogError(exMsg2, "Failed to send phrase by any known method");
					}
				}
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in HubClientService loop. Will retry in 5s.");
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		try
		{
			if (_connection != null)
			{
				await _connection.StopAsync(cancellationToken);
				await _connection.DisposeAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while stopping HubClientService");
		}
	}
}


