using System;

namespace VehicleIntelligence.Application.Events;

/// <summary>
/// Event published by the Worker service when a vehicle alert rule is triggered.
/// </summary>
public sealed record AlertTriggeredEvent
{
    public Guid AlertId { get; init; }
    public Guid VehicleId { get; init; }
    public string VehicleExternalId { get; init; } = string.Empty;
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public double RiskScore { get; init; }
    public DateTime Timestamp { get; init; }
}
