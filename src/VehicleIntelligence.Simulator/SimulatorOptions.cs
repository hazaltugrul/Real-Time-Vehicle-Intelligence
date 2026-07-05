namespace VehicleIntelligence.Simulator;

/// <summary>
/// Configuration for the CSV-to-gRPC simulator.
/// Loaded from appsettings.json Simulator section.
/// </summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string CsvPath { get; set; } = "data/vehicle-energy-telemetry.csv";
    public string GrpcEndpoint { get; set; } = "http://localhost:5000";
    public int DelayMilliseconds { get; set; } = 500;
    public bool Loop { get; set; } = false;
    public int MaxRows { get; set; } = 10000;
}

/// <summary>
/// Configurable column name mapping from CSV headers to domain fields.
/// Allows the simulator to work with any dataset column naming convention.
/// </summary>
public sealed class CsvMappingOptions
{
    public const string SectionName = "CsvMapping";

    public string VehicleId { get; set; } = "Vehicle_ID";
    public string TripId { get; set; } = "Trip_ID";
    public string Timestamp { get; set; } = "Timestamp";
    public string Speed { get; set; } = "Speed";
    public string Latitude { get; set; } = "Latitude";
    public string Longitude { get; set; } = "Longitude";
    public string BatteryLevel { get; set; } = "SOC";
    public string EngineRpm { get; set; } = "RPM";
    public string EngineLoad { get; set; } = "Engine_Load";
    public string FuelRate { get; set; } = "Fuel_Rate";
    public string EnergyConsumption { get; set; } = "Energy_Consumption";
    public string BatteryVoltage { get; set; } = "Battery_Voltage";
    public string BatteryCurrent { get; set; } = "Battery_Current";
    public string Temperature { get; set; } = "Outside_Air_Temp";
    public string Distance { get; set; } = "Distance";
    public string MassAirFlow { get; set; } = "MAF";
    public string AirConditioningPower { get; set; } = "ACPower";
    public string HeaterPower { get; set; } = "HeaterPower";
    public string Elevation { get; set; } = "Elevation";
    public string SpeedLimit { get; set; } = "SpeedLimit";
}
