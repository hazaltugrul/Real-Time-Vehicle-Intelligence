namespace VehicleIntelligence.Application.DTOs;

public sealed record TelemetryRecordDto(
    Guid Id,
    Guid VehicleId,
    string? TripId,
    DateTime Timestamp,
    double? Speed,
    double? Latitude,
    double? Longitude,
    double? BatteryLevel,
    double? EngineRpm,
    double? EngineLoad,
    double? FuelRate,
    double? EnergyConsumption,
    double? Temperature,
    double? Distance,
    double RiskScore,
    DateTime CreatedAt
);
