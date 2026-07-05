using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Domain.Interfaces;

public interface ITelemetryRepository
{
    Task<TelemetryRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TelemetryRecord?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<TelemetryRecord> Items, int TotalCount)> GetByVehicleIdPagedAsync(
        Guid vehicleId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<TelemetryRecord> AddAsync(TelemetryRecord record, CancellationToken cancellationToken = default);
    Task<long> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<double> GetAverageRiskScoreAsync(CancellationToken cancellationToken = default);
}
