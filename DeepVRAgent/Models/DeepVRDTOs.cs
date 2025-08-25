namespace DeepVRAgent.Models;

public class MetricsMessage
{
	public required string Uuid { get; set; }
	public float Cpu { get; set; }
	public float Ram { get; set; }
	public required string Ip { get; set; }
	public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
