using System.Net.Http.Json;
using AdminP.Shared;

namespace AdminP.Services;

public class ComputerService : IComputerService
{
    private readonly HttpClient _httpClient;
    private readonly List<Computer> _computers = new();
    private bool _initialized;

    public event Action? OnChanged;

    public IReadOnlyList<Computer> Computers => _computers;

    public ComputerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;
        _computers.Clear();
        try
        {
            var response = await _httpClient.GetAsync("api/pc", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<Computer>>(cancellationToken: cancellationToken);
                if (list != null)
                {
                    _computers.AddRange(list);
                }
                _initialized = true;
                OnChanged?.Invoke();
            }
        }
        catch
        {
            // ignore for now; UI will show empty list
        }
    }

    public async Task<List<Computer>> GetComputersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/pc", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<List<Computer>>(cancellationToken: cancellationToken);
                return list ?? new List<Computer>();
            }
        }
        catch
        {
            // ignore
        }

        return new List<Computer>();
    }

    public async Task AddComputerAsync(Guid uuid, string name, string ip, string hostName, CancellationToken cancellationToken = default)
    {
        var dto = new PcDto { Uuid = uuid, Name = name, Ip = ip, HostName = hostName };
        try
        {
            var responce = await _httpClient.PostAsJsonAsync("api/pc", dto);
            if (!responce.IsSuccessStatusCode)
            {
                Console.WriteLine("Ошибка подключения/удаления ");
            }

            if (responce.IsSuccessStatusCode)
            {
                // Optionally reload list from backend, for now push locally
                _computers.Add(new Computer { Uuid = dto.Uuid, Name = dto.Name, IP = dto.Ip });
                OnChanged?.Invoke();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка подключения");
        }
    }

    public async Task AddComputerByUuidAsync(Guid uuid, string name, string ip, string hostName,
        CancellationToken cancellationToken = default)
    {
        var dto = new PcDto { Uuid = uuid, Name = name, Ip = ip, HostName = hostName };
        try
        {
            var responce = await _httpClient.PostAsJsonAsync("api/pc", dto);
            if (!responce.IsSuccessStatusCode)
            {
                Console.WriteLine("Ошибка подключения/удаления ");
            }

            if (_computers.All(c => c.Uuid != uuid) && responce.IsSuccessStatusCode)
            {
                _computers.Add(new Computer { Uuid = dto.Uuid, Name = dto.Name, IP = dto.Ip });
                OnChanged?.Invoke();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка");
        }
    }

    public async Task DeleteComputerAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var found = _computers.FirstOrDefault(c => c.Uuid == uuid);
        try
        {
            var responce = await _httpClient.DeleteAsync($"api/pc/{found.Uuid}");
            if (!responce.IsSuccessStatusCode)
            {
                Console.WriteLine("Ошибка подключения/удаления ");
            }

            if (responce.IsSuccessStatusCode)
            {
                _computers.Remove(found);
                OnChanged?.Invoke();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка");
        }
    }

    public async Task<Computer?> GetByUuidAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        return _computers.FirstOrDefault(c => c.Uuid == uuid);
    }

    public void ApplyMetrics(ComputerMetrics metrics)
    {
        var computer = _computers.FirstOrDefault(c => c.Uuid == metrics.Uuid || c.IP == metrics.Ip);
        if (computer == null) return;

        computer.LatestMetrics = metrics;
        computer.LastSeen = metrics.ReceivedAt == default ? DateTime.Now : metrics.ReceivedAt;
        OnChanged?.Invoke();
    }
}