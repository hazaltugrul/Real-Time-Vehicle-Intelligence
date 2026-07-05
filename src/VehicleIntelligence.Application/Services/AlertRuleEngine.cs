using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Application.Services;

/// <summary>
/// Evaluates a telemetry record against predefined business rules and returns any triggered alerts.
/// This is rule-based only — no ML in Phase 1.
/// Designed for easy extension: add new rule methods following the existing pattern.
/// </summary>
public interface IAlertRuleEngine
{
    IEnumerable<Alert> Evaluate(TelemetryRecord record, double riskScore);
}

public sealed class AlertRuleEngine : IAlertRuleEngine
{
    // Rule thresholds
    private const double OverspeedThreshold = 130.0;
    private const double HighTempWarning = 100.0;
    private const double HighTempCritical = 115.0;
    private const double LowBatteryMedium = 20.0;
    private const double LowBatteryHigh = 10.0;
    private const double AbnormalEnergyThreshold = 500.0; // Wh/km

    public IEnumerable<Alert> Evaluate(TelemetryRecord record, double riskScore)
    {
        var alerts = new List<Alert>();

        CheckOverspeed(record, riskScore, alerts);
        CheckHighTemperature(record, riskScore, alerts);
        CheckLowBattery(record, riskScore, alerts);
        CheckAbnormalEnergy(record, riskScore, alerts);
        CheckInvalidTelemetry(record, riskScore, alerts);

        return alerts;
    }

    private static void CheckOverspeed(TelemetryRecord record, double riskScore, List<Alert> alerts)
    {
        if (record.Speed is null) return;

        double limit = record.SpeedLimit ?? OverspeedThreshold;
        if (record.Speed <= limit) return;

        alerts.Add(Alert.Create(
            record.VehicleId,
            AlertType.Overspeed,
            AlertSeverity.High,
            $"Vehicle speed {record.Speed:F1} km/h exceeds limit of {limit:F1} km/h.",
            riskScore,
            record.Id));
    }

    private static void CheckHighTemperature(TelemetryRecord record, double riskScore, List<Alert> alerts)
    {
        if (record.Temperature is null || record.Temperature <= HighTempWarning) return;

        var severity = record.Temperature >= HighTempCritical
            ? AlertSeverity.Critical
            : AlertSeverity.High;

        alerts.Add(Alert.Create(
            record.VehicleId,
            AlertType.HighTemperature,
            severity,
            $"Temperature {record.Temperature:F1}°C exceeds safe limit.",
            riskScore,
            record.Id));
    }

    private static void CheckLowBattery(TelemetryRecord record, double riskScore, List<Alert> alerts)
    {
        if (record.BatteryLevel is null || record.BatteryLevel > LowBatteryMedium) return;

        var severity = record.BatteryLevel <= LowBatteryHigh
            ? AlertSeverity.High
            : AlertSeverity.Medium;

        alerts.Add(Alert.Create(
            record.VehicleId,
            AlertType.LowBattery,
            severity,
            $"Battery level at {record.BatteryLevel:F1}% — {(record.BatteryLevel <= LowBatteryHigh ? "critically" : "")} low.",
            riskScore,
            record.Id));
    }

    private static void CheckAbnormalEnergy(TelemetryRecord record, double riskScore, List<Alert> alerts)
    {
        if (record.EnergyConsumption is null || record.EnergyConsumption <= AbnormalEnergyThreshold) return;

        alerts.Add(Alert.Create(
            record.VehicleId,
            AlertType.AbnormalEnergyConsumption,
            AlertSeverity.Medium,
            $"Energy consumption {record.EnergyConsumption:F2} Wh/km exceeds threshold of {AbnormalEnergyThreshold} Wh/km.",
            riskScore,
            record.Id));
    }

    private static void CheckInvalidTelemetry(TelemetryRecord record, double riskScore, List<Alert> alerts)
    {
        var issues = new List<string>();

        if (record.Speed is < 0) issues.Add("negative speed");
        if (record.Latitude is < -90 or > 90) issues.Add("invalid latitude");
        if (record.Longitude is < -180 or > 180) issues.Add("invalid longitude");
        if (record.BatteryLevel is < 0 or > 100) issues.Add("battery level out of range");

        if (issues.Count == 0) return;

        alerts.Add(Alert.Create(
            record.VehicleId,
            AlertType.InvalidTelemetry,
            AlertSeverity.Low,
            $"Invalid telemetry detected: {string.Join(", ", issues)}.",
            riskScore,
            record.Id));
    }
}
