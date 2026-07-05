using Microsoft.EntityFrameworkCore;
using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Vehicle Intelligence Platform.
/// Uses PostgreSQL as the underlying database provider.
/// </summary>
public sealed class VehicleIntelligenceDbContext : DbContext
{
    public VehicleIntelligenceDbContext(DbContextOptions<VehicleIntelligenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleIntelligenceDbContext).Assembly);
    }
}
