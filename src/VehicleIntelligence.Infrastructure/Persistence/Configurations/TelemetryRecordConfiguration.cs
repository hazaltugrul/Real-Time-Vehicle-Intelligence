using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class TelemetryRecordConfiguration : IEntityTypeConfiguration<TelemetryRecord>
{
    public void Configure(EntityTypeBuilder<TelemetryRecord> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TripId).HasMaxLength(100);
        builder.Property(t => t.RawPayloadJson).HasColumnType("text");

        // Composite index for time-range queries per vehicle (most common access pattern)
        builder.HasIndex(t => new { t.VehicleId, t.Timestamp })
            .HasDatabaseName("IX_TelemetryRecords_VehicleId_Timestamp");

        builder.HasIndex(t => t.Timestamp)
            .HasDatabaseName("IX_TelemetryRecords_Timestamp");

        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.TelemetryRecords)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("TelemetryRecords");
    }
}
