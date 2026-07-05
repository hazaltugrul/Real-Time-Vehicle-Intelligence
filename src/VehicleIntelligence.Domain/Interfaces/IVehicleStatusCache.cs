using VehicleIntelligence.Domain.ValueObjects;

namespace VehicleIntelligence.Domain.Interfaces;

public interface IVehicleStatusCache
{
    Task<VehicleLatestStatus?> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task SetAsync(VehicleLatestStatus status, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
