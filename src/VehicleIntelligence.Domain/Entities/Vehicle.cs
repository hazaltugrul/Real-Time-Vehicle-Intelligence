using VehicleIntelligence.Domain.Common;
using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Domain.Entities;

/// <summary>
/// Represents a physical or virtual vehicle tracked by the platform.
/// </summary>
public class Vehicle : BaseEntity
{
    /// <summary>External identifier coming from telemetry stream (e.g., vehicle VIN or dataset ID).</summary>
    public string VehicleExternalId { get; private set; } = string.Empty;

    public DateTime LastSeenAt { get; private set; }

    public VehicleStatus Status { get; private set; } = VehicleStatus.Unknown;

    // Navigation properties
    public ICollection<TelemetryRecord> TelemetryRecords { get; private set; } = new List<TelemetryRecord>();
    public ICollection<Alert> Alerts { get; private set; } = new List<Alert>();

    // Required by EF Core
    private Vehicle() { }

    public static Vehicle Create(string vehicleExternalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vehicleExternalId);

        return new Vehicle
        {
            VehicleExternalId = vehicleExternalId,
            LastSeenAt = DateTime.UtcNow,
            Status = VehicleStatus.Active
        };
    }

    public void UpdateLastSeen(DateTime timestamp)
    {
        LastSeenAt = timestamp > LastSeenAt ? timestamp : DateTime.UtcNow;
        Status = VehicleStatus.Active;
    }

    public void MarkPassive()
    {
        Status = VehicleStatus.Passive;
    }
}
