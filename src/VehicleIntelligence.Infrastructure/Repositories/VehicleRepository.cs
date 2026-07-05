using Microsoft.EntityFrameworkCore;
using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Infrastructure.Persistence;

namespace VehicleIntelligence.Infrastructure.Repositories;

internal sealed class VehicleRepository : IVehicleRepository
{
    private readonly VehicleIntelligenceDbContext _context;

    public VehicleRepository(VehicleIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Vehicle?> GetByExternalIdAsync(string vehicleExternalId, CancellationToken cancellationToken = default)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleExternalId == vehicleExternalId, cancellationToken);

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<Domain.Enums.VehicleStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(v => v.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.LastSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        => await _context.Vehicles.CountAsync(cancellationToken);

    public async Task<int> GetOnlineCountAsync(TimeSpan onlineThreshold, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - onlineThreshold;
        return await _context.Vehicles.CountAsync(v => v.LastSeenAt >= cutoff, cancellationToken);
    }
}
