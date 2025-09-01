using AdminP.Shared;

namespace AdminP.Services;

public interface ISessionService
{
    Task<List<Session>> GetSessionsAsync(CancellationToken cancellationToken = default);
}


