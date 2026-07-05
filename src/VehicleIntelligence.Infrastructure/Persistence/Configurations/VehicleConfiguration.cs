using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VehicleExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(v => v.VehicleExternalId)
            .IsUnique()
            .HasDatabaseName("IX_Vehicles_ExternalId");

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(v => v.TelemetryRecords)
            .WithOne(t => t.Vehicle)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Alerts)
            .WithOne(a => a.Vehicle)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Vehicles");
    }
}
