namespace AdminP.Shared;

public class ComputerMetrics
{
    public Guid Uuid { get; set; }
    public float Cpu { get; set; }
    public float Ram { get; set; }
    public string Ip { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}