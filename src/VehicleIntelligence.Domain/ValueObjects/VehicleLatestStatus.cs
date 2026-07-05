using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Domain.ValueObjects;

/// <summary>
/// Value object representing the latest known state of a vehicle.
/// This is cached in Redis and does not have a database table.
/// </summary>
public sealed class VehicleLatestStatus
{
    public Guid VehicleId { get; init; }
    public DateTime LastTelemetryTimestamp { get; init; }
    public double? Speed { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? BatteryLevel { get; init; }
    public double? Temperature { get; init; }
    public double RiskScore { get; init; }
    public ConnectionStatus ConnectionStatus { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static VehicleLatestStatus FromTelemetry(
        Guid vehicleId,
        DateTime timestamp,
        double? speed,
        double? latitude,
        double? longitude,
        double? batteryLevel,
        double? temperature,
        double riskScore,
        ConnectionStatus connectionStatus)
    {
        return new VehicleLatestStatus
        {
            VehicleId = vehicleId,
            LastTelemetryTimestamp = timestamp,
            Speed = speed,
            Latitude = latitude,
            Longitude = longitude,
            BatteryLevel = batteryLevel,
            Temperature = temperature,
            RiskScore = riskScore,
            ConnectionStatus = connectionStatus,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
