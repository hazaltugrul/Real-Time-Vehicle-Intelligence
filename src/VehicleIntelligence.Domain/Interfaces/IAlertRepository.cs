using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Domain.Interfaces;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Alert> Items, int TotalCount)> GetPagedAsync(
        Guid? vehicleId,
        AlertSeverity? severity,
        AlertType? alertType,
        bool? isResolved,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default);
    Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCriticalOpenCountAsync(CancellationToken cancellationToken = default);
}
