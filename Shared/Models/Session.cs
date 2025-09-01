namespace AdminP.Shared;

public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PkId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
}