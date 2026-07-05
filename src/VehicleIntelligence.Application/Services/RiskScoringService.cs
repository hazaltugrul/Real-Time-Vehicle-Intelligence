using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;

namespace VehicleIntelligence.Application.Services;

/// <summary>
/// Calculates a composite risk score (0-100) for a telemetry record.
/// Score composition:
///   Speed risk:       up to 30 pts
///   Temperature risk: up to 30 pts
///   Battery risk:     up to 20 pts
///   Data validity:    up to 20 pts
/// </summary>
public interface IRiskScoringService
{
    double Calculate(TelemetryRecord record);
}

public sealed class RiskScoringService : IRiskScoringService
{
    // Thresholds
    private const double SpeedWarning = 100.0;
    private const double SpeedMax = 160.0;
    private const double TempWarning = 80.0;
    private const double TempCritical = 115.0;
    private const double BatteryLow = 20.0;
    private const double BatteryCritical = 10.0;

    public double Calculate(TelemetryRecord record)
    {
        var speedRisk = CalculateSpeedRisk(record.Speed);
        var tempRisk = CalculateTemperatureRisk(record.Temperature);
        var batteryRisk = CalculateBatteryRisk(record.BatteryLevel);
        var validityRisk = CalculateValidityRisk(record);

        return Math.Min(100, speedRisk + tempRisk + batteryRisk + validityRisk);
    }

    private static double CalculateSpeedRisk(double? speed)
    {
        if (speed is null or < 0) return 15; // invalid speed
        if (speed < SpeedWarning) return 0;
        if (speed >= SpeedMax) return 30;
        // Linear interpolation between warning and max
        return 30 * ((speed.Value - SpeedWarning) / (SpeedMax - SpeedWarning));
    }

    private static double CalculateTemperatureRisk(double? temperature)
    {
        if (temperature is null) return 0;
        if (temperature < TempWarning) return 0;
        if (temperature >= TempCritical) return 30;
        return 30 * ((temperature.Value - TempWarning) / (TempCritical - TempWarning));
    }

    private static double CalculateBatteryRisk(double? batteryLevel)
    {
        if (batteryLevel is null) return 0;
        if (batteryLevel > BatteryLow) return 0;
        if (batteryLevel <= BatteryCritical) return 20;
        return 20 * ((BatteryLow - batteryLevel.Value) / (BatteryLow - BatteryCritical));
    }

    private static double CalculateValidityRisk(TelemetryRecord record)
    {
        var penalties = 0.0;

        if (record.Latitude is null || record.Longitude is null) penalties += 8;
        if (record.Speed is < 0) penalties += 7;
        if (record.BatteryLevel is < 0 or > 100) penalties += 5;

        return Math.Min(20, penalties);
    }
}
