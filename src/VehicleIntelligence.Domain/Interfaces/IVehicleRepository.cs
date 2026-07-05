using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Domain.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByExternalIdAsync(string vehicleExternalId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? statusFilter = null,
        CancellationToken cancellationToken = default);
    Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetOnlineCountAsync(TimeSpan onlineThreshold, CancellationToken cancellationToken = default);
}
