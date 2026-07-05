namespace VehicleIntelligence.Application.Events;

/// <summary>
/// Event published to RabbitMQ when a valid telemetry message is received via gRPC.
/// This event is consumed by the Worker service for persistence and processing.
/// </summary>
public sealed record TelemetryReceivedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string VehicleExternalId { get; init; } = string.Empty;
    public string? TripId { get; init; }
    public DateTime Timestamp { get; init; }
    public double? Speed { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? BatteryLevel { get; init; }
    public double? BatteryVoltage { get; init; }
    public double? BatteryCurrent { get; init; }
    public double? EngineRpm { get; init; }
    public double? EngineLoad { get; init; }
    public double? FuelRate { get; init; }
    public double? EnergyConsumption { get; init; }
    public double? Temperature { get; init; }
    public double? Distance { get; init; }
    public double? MassAirFlow { get; init; }
    public double? AirConditioningPower { get; init; }
    public double? HeaterPower { get; init; }
    public double? Elevation { get; init; }
    public double? SpeedLimit { get; init; }
    public string? RawPayloadJson { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
