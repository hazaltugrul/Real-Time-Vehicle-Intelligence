using System;

namespace VehicleIntelligence.Application.Events;

/// <summary>
/// Event published by the Worker service after telemetry record processing and persistence is completed.
/// </summary>
public sealed record TelemetryProcessedEvent
{
    public Guid RecordId { get; init; }
    public Guid VehicleId { get; init; }
    public string VehicleExternalId { get; init; } = string.Empty;
    public string? TripId { get; init; }
    public DateTime Timestamp { get; init; }
    public double Speed { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double BatteryLevel { get; init; }
    public double Temperature { get; init; }
    public double RiskScore { get; init; }
    public double? EngineRpm { get; init; }
    public double? EngineLoad { get; init; }
    public double? FuelRate { get; init; }
    public double? EnergyConsumption { get; init; }
    public double? BatteryVoltage { get; init; }
    public double? BatteryCurrent { get; init; }
    public double? Elevation { get; init; }
    public double? SpeedLimit { get; init; }
}
