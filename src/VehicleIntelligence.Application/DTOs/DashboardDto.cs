using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Application.DTOs;

public sealed record VehicleLatestStatusDto(
    Guid VehicleId,
    DateTime LastTelemetryTimestamp,
    double? Speed,
    double? Latitude,
    double? Longitude,
    double? BatteryLevel,
    double? Temperature,
    double RiskScore,
    string ConnectionStatus,
    DateTime UpdatedAt
);

public sealed record DashboardSummaryDto(
    int TotalVehicles,
    int OnlineVehicles,
    long TelemetryLast24Hours,
    int OpenAlerts,
    int CriticalAlerts,
    double AverageRiskScore
);
