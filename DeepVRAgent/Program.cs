using DeepVRAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var hostBuilder = Host.CreateDefaultBuilder(args)
	.ConfigureLogging(logging =>
	{
		logging.ClearProviders();
		logging.AddConsole();
		logging.AddDebug();
		logging.SetMinimumLevel(LogLevel.Information);
	})
	.ConfigureServices((context, services) =>
	{
		services.AddSingleton<MetricsCollector>();

		// Фоновый клиент SignalR, который подключается к удалённому хабу
		services.AddHostedService<HubClientService>();
	});

var host = hostBuilder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting DeepVR Agent in CLIENT mode (SignalR)");

await host.RunAsync();
