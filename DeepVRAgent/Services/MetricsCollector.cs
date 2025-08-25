using System.Management;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Net;
using DeepVRAgent.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeepVRAgent.Services;

[SupportedOSPlatform("windows")]
public class MetricsCollector
{
	private readonly ILogger<MetricsCollector> _logger;

	public MetricsCollector(ILogger<MetricsCollector> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public MetricsMessage GetMetrics()
	{
		try
		{
			_logger.LogInformation("=== METRICS COLLECTION START ===");
			_logger.LogInformation("Starting metrics collection...");
			var uuid = GetUUID();
			_logger.LogInformation("Retrieved UUID: {UUID}", uuid);
			var cpu = GetCPU();
			_logger.LogInformation("Retrieved CPU: {CPU}%", cpu);
			var ram = GetRAM();
			_logger.LogInformation("Retrieved RAM: {RAM}%", ram);
			var ip = GetLocalIpAddress();
			_logger.LogInformation("Retrieved IP: {IP}", ip);

			var metric = new MetricsMessage
			{
				Uuid = uuid,
				Cpu = cpu,
				Ram = ram,
				Ip = ip,
				ReceivedAt = DateTime.UtcNow
			};
			_logger.LogInformation("Metrics collected successfully: {Metric}", JsonConvert.SerializeObject(metric));
			_logger.LogInformation("=== METRICS COLLECTION END ===");
			return metric;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error collecting metrics");
			_logger.LogInformation("=== METRICS COLLECTION ERROR ===");
			return new MetricsMessage { Uuid = "Error", Cpu = 0, Ram = 0, Ip = "0.0.0.0", ReceivedAt = DateTime.UtcNow };
		}
	}

	public string GetUUID()
	{
		try
		{
			using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
			using var collection = searcher.Get();
			foreach (ManagementObject obj in collection)
			{
				return obj["UUID"]?.ToString() ?? "Unknown";
			}
			return "Unknown";
		}
		catch (Exception)
		{
			return "Unknown";
		}
	}

	private string GetLocalIpAddress()
	{
		try
		{
			string hostName = Dns.GetHostName();
			var entry = Dns.GetHostEntry(hostName);
			var ip = entry.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
			return ip?.ToString() ?? "127.0.0.1";
		}
		catch
		{
			return "127.0.0.1";
		}
	}

	private float GetCPU()
	{
		try
		{
			using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			cpuCounter.NextValue();
			Thread.Sleep(100);
			var cpuUsage = cpuCounter.NextValue();
			_logger.LogDebug("CPU Usage: {CPU}%", cpuUsage);
			return cpuUsage;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to get CPU metrics");
			try
			{
				using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
				using var collection = searcher.Get();
				foreach (ManagementObject obj in collection)
				{
					var load = Convert.ToSingle(obj["LoadPercentage"]);
					_logger.LogDebug("CPU Usage (WMI): {CPU}%", load);
					return load;
				}
			}
			catch (Exception wmiEx)
			{
				_logger.LogError(wmiEx, "WMI fallback also failed");
			}
			return 0.0f;
		}
	}

	private float GetRAM()
	{
		try
		{
			using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");
			using var collection = searcher.Get();
			foreach (ManagementObject obj in collection)
			{
				var total = Convert.ToSingle(obj["TotalVisibleMemorySize"]) / 1024;
				var free = Convert.ToSingle(obj["FreePhysicalMemory"]) / 1024;
				var ramUsage = 100 - (free / total * 100);
				_logger.LogDebug("RAM Usage (WMI): {RAM}% (Free: {Free}MB, Total: {Total}MB)", ramUsage, free, total);
				return ramUsage;
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to get RAM metrics via WMI, trying PerformanceCounter");
			try
			{
				using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
				var availableRam = ramCounter.NextValue();
				var totalRam = GetTotalRAMViaWMI();
				if (totalRam > 0)
				{
					var ramUsage = 100 - (availableRam / totalRam * 100);
					_logger.LogDebug("RAM Usage (PerformanceCounter): {RAM}% (Available: {Available}MB, Total: {Total}MB)", ramUsage, availableRam, totalRam);
					return ramUsage;
				}
			}
			catch (Exception perfEx)
			{
				_logger.LogError(perfEx, "PerformanceCounter fallback also failed");
			}
		}
		return 0.0f;
	}

	private float GetTotalRAMViaWMI()
	{
		try
		{
			using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
			using var collection = searcher.Get();
			foreach (ManagementObject obj in collection)
			{
				return Convert.ToSingle(obj["TotalVisibleMemorySize"]) / 1024;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get total RAM via WMI");
		}
		return 0.0f;
	}
}