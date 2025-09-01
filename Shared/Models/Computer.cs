namespace AdminP.Shared;

public class Computer
{
    public Guid Uuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IP { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsInUse { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.MinValue;
    public ComputerMetrics? LatestMetrics { get; set; }
}