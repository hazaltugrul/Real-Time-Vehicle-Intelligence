using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleIntelligence.Domain.Entities;

namespace VehicleIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AlertType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(500);

        // Indexes for common filter queries
        builder.HasIndex(a => a.VehicleId)
            .HasDatabaseName("IX_Alerts_VehicleId");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_Alerts_CreatedAt");

        builder.HasIndex(a => a.Severity)
            .HasDatabaseName("IX_Alerts_Severity");

        builder.HasIndex(a => a.IsResolved)
            .HasDatabaseName("IX_Alerts_IsResolved");

        builder.HasOne(a => a.Vehicle)
            .WithMany(v => v.Alerts)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.TelemetryRecord)
            .WithMany(t => t.Alerts)
            .HasForeignKey(a => a.TelemetryRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("Alerts");
    }
}
