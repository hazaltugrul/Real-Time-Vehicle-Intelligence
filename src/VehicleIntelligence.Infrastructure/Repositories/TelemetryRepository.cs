using Microsoft.EntityFrameworkCore;
using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Infrastructure.Persistence;

namespace VehicleIntelligence.Infrastructure.Repositories;

internal sealed class TelemetryRepository : ITelemetryRepository
{
    private readonly VehicleIntelligenceDbContext _context;

    public TelemetryRepository(VehicleIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<TelemetryRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.TelemetryRecords.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<TelemetryRecord?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        => await _context.TelemetryRecords
            .Where(t => t.VehicleId == vehicleId)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IEnumerable<TelemetryRecord> Items, int TotalCount)> GetByVehicleIdPagedAsync(
        Guid vehicleId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TelemetryRecords
            .Where(t => t.VehicleId == vehicleId);

        if (from.HasValue)
            query = query.Where(t => t.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Timestamp <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<TelemetryRecord> AddAsync(TelemetryRecord record, CancellationToken cancellationToken = default)
    {
        await _context.TelemetryRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<long> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        => await _context.TelemetryRecords.LongCountAsync(t => t.CreatedAt >= since, cancellationToken);

    public async Task<double> GetAverageRiskScoreAsync(CancellationToken cancellationToken = default)
    {
        var hasRecords = await _context.TelemetryRecords.AnyAsync(cancellationToken);
        if (!hasRecords) return 0;

        return await _context.TelemetryRecords.AverageAsync(t => t.RiskScore, cancellationToken);
    }
}
