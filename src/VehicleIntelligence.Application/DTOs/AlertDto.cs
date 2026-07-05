namespace VehicleIntelligence.Application.DTOs;

public sealed record AlertDto(
    Guid Id,
    Guid VehicleId,
    Guid? TelemetryRecordId,
    string AlertType,
    string Severity,
    string Message,
    double RiskScore,
    bool IsResolved,
    DateTime? ResolvedAt,
    DateTime CreatedAt
);
