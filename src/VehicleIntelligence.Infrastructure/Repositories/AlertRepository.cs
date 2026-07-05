using Microsoft.EntityFrameworkCore;
using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Infrastructure.Persistence;

namespace VehicleIntelligence.Infrastructure.Repositories;

internal sealed class AlertRepository : IAlertRepository
{
    private readonly VehicleIntelligenceDbContext _context;

    public AlertRepository(VehicleIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IEnumerable<Alert> Items, int TotalCount)> GetPagedAsync(
        Guid? vehicleId,
        AlertSeverity? severity,
        AlertType? alertType,
        bool? isResolved,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Alerts.AsQueryable();

        if (vehicleId.HasValue)
            query = query.Where(a => a.VehicleId == vehicleId.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (alertType.HasValue)
            query = query.Where(a => a.AlertType == alertType.Value);

        if (isResolved.HasValue)
            query = query.Where(a => a.IsResolved == isResolved.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Alert> AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await _context.Alerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _context.Alerts.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default)
        => await _context.Alerts.CountAsync(a => !a.IsResolved, cancellationToken);

    public async Task<int> GetCriticalOpenCountAsync(CancellationToken cancellationToken = default)
        => await _context.Alerts.CountAsync(a => !a.IsResolved && a.Severity == AlertSeverity.Critical, cancellationToken);
}
