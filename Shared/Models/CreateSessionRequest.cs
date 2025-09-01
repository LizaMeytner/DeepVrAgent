namespace AdminP.Shared;

public class CreateSessionRequest
{
    public int UserId { get; set; }
    public int PkId { get; set; }
    public int Duration { get; set; }
}