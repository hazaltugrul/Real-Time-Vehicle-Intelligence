using VehicleIntelligence.Domain.Common;

namespace VehicleIntelligence.Domain.Entities;

/// <summary>
/// Represents a single telemetry snapshot from a vehicle.
/// All nullable fields reflect that not every vehicle/dataset provides all sensor values.
/// </summary>
public class TelemetryRecord : BaseEntity
{
    public Guid VehicleId { get; private set; }

    public string? TripId { get; private set; }

    public DateTime Timestamp { get; private set; }

    // Motion
    public double? Speed { get; private set; }           // km/h
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? Distance { get; private set; }        // km

    // Battery / Electric
    public double? BatteryLevel { get; private set; }   // % SOC
    public double? BatteryVoltage { get; private set; } // V
    public double? BatteryCurrent { get; private set; } // A

    // Engine / ICE
    public double? EngineRpm { get; private set; }
    public double? EngineLoad { get; private set; }      // %
    public double? FuelRate { get; private set; }        // L/h

    // Energy
    public double? EnergyConsumption { get; private set; } // Wh/km or kWh

    // Environment
    public double? Temperature { get; private set; }    // °C

    // Kaggle Specific Telemetry
    public double? MassAirFlow { get; private set; }
    public double? AirConditioningPower { get; private set; }
    public double? HeaterPower { get; private set; }
    public double? Elevation { get; private set; }
    public double? SpeedLimit { get; private set; }

    // Raw payload for auditing / future extensibility
    public string? RawPayloadJson { get; private set; }

    // Computed risk score (set by worker after processing)
    public double RiskScore { get; private set; }

    // Navigation
    public Vehicle Vehicle { get; private set; } = null!;
    public ICollection<Alert> Alerts { get; private set; } = new List<Alert>();

    private TelemetryRecord() { }

    public static TelemetryRecord Create(
        Guid vehicleId,
        DateTime timestamp,
        string? tripId = null,
        double? speed = null,
        double? latitude = null,
        double? longitude = null,
        double? distance = null,
        double? batteryLevel = null,
        double? batteryVoltage = null,
        double? batteryCurrent = null,
        double? engineRpm = null,
        double? engineLoad = null,
        double? fuelRate = null,
        double? energyConsumption = null,
        double? temperature = null,
        double? massAirFlow = null,
        double? airConditioningPower = null,
        double? heaterPower = null,
        double? elevation = null,
        double? speedLimit = null,
        string? rawPayloadJson = null)
    {
        return new TelemetryRecord
        {
            VehicleId = vehicleId,
            Timestamp = timestamp,
            TripId = tripId,
            Speed = speed,
            Latitude = latitude,
            Longitude = longitude,
            Distance = distance,
            BatteryLevel = batteryLevel,
            BatteryVoltage = batteryVoltage,
            BatteryCurrent = batteryCurrent,
            EngineRpm = engineRpm,
            EngineLoad = engineLoad,
            FuelRate = fuelRate,
            EnergyConsumption = energyConsumption,
            Temperature = temperature,
            MassAirFlow = massAirFlow,
            AirConditioningPower = airConditioningPower,
            HeaterPower = heaterPower,
            Elevation = elevation,
            SpeedLimit = speedLimit,
            RawPayloadJson = rawPayloadJson
        };
    }

    public void SetRiskScore(double riskScore)
    {
        RiskScore = Math.Clamp(riskScore, 0, 100);
    }
}
