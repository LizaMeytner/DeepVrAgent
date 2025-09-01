using AdminP.Shared;

namespace AdminP.Services;

public interface IComputerService
{
    event Action? OnChanged;

    IReadOnlyList<Computer> Computers { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<List<Computer>> GetComputersAsync(CancellationToken cancellationToken = default);

    Task AddComputerAsync(Guid uuid, string name, string ip, string hostName, CancellationToken cancellationToken = default);

    Task AddComputerByUuidAsync(Guid uuid, string name, string ip, string hostName, CancellationToken cancellationToken = default);

    Task DeleteComputerAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<Computer?> GetByUuidAsync(Guid uuid, CancellationToken cancellationToken = default);

    void ApplyMetrics(ComputerMetrics metrics);
}


