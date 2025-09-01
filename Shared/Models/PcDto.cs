namespace AdminP.Shared;

public class PcDto
{
    public Guid Uuid { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Name  { get; set; } = string.Empty;
}