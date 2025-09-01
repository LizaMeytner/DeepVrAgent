using System.Net.Http.Json;
using AdminP.Shared;

namespace AdminP.Services;

public class SessionService : ISessionService
{
    private readonly HttpClient _httpClient;
    public SessionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Session>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        // mock
        return new List<Session>();
    }
}


