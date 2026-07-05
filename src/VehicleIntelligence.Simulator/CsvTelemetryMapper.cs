using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleIntelligence.Grpc;
using static System.FormattableString;

namespace VehicleIntelligence.Simulator;

/// <summary>
/// Reads a CSV file and maps rows to TelemetryMessage proto objects.
/// Column mapping is fully configurable via CsvMappingOptions.
/// Resilient to missing columns — unmapped fields are sent as absent (has_* = false).
/// </summary>
public sealed class CsvTelemetryMapper
{
    private readonly CsvMappingOptions _mapping;
    private readonly ILogger<CsvTelemetryMapper> _logger;
    private readonly DateTime _baseTime = DateTime.UtcNow.AddMinutes(-30);

    public CsvTelemetryMapper(IOptions<CsvMappingOptions> mapping, ILogger<CsvTelemetryMapper> logger)
    {
        _mapping = mapping.Value;
        _logger = logger;
    }

    public IEnumerable<TelemetryMessage> ReadMessages(string csvPath, int maxRows)
    {
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("CSV dataset not found at '{Path}'. Generating a realistic mock dataset to enable out-of-the-box streaming...", csvPath);
            try
            {
                GenerateMockCsv(csvPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate mock CSV at '{Path}'", csvPath);
                throw;
            }
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null, // Don't throw on missing columns
            BadDataFound = null
        };

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        _logger.LogInformation("CSV headers detected: {Headers}", string.Join(", ", headers));

        var rowCount = 0;
        while (csv.Read() && rowCount < maxRows)
        {
            TelemetryMessage? message = null;
            try
            {
                message = MapRow(csv, headers);
                rowCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse CSV row {Row}", rowCount + 1);
                continue;
            }

            yield return message;
        }

        _logger.LogInformation("CSV reading complete. Total rows read: {Count}", rowCount);
    }

    private TelemetryMessage MapRow(CsvReader csv, string[] headers)
    {
        var msg = new TelemetryMessage();

        msg.VehicleId = GetString(csv, headers, _mapping.VehicleId) ?? $"VEHICLE_{GetString(csv, headers, "Vehicle_ID") ?? Guid.NewGuid().ToString("N")[..8]}";
        msg.TripId = GetString(csv, headers, _mapping.TripId) ?? string.Empty;
        
        var rawTime = GetString(csv, headers, _mapping.Timestamp);
        if (!string.IsNullOrWhiteSpace(rawTime) && double.TryParse(rawTime, NumberStyles.Any, CultureInfo.InvariantCulture, out var ms))
        {
            msg.Timestamp = _baseTime.AddMilliseconds(ms).ToString("o");
        }
        else
        {
            msg.Timestamp = rawTime ?? DateTime.UtcNow.ToString("o");
        }

        msg.RawPayloadJson = BuildRawJson(csv, headers);

        // Speed
        if (TryGetDouble(csv, headers, _mapping.Speed, out var speed))
        {
            msg.Speed = speed;
            msg.HasSpeed = true;
        }

        // Location
        if (TryGetDouble(csv, headers, _mapping.Latitude, out var lat))
        {
            msg.Latitude = lat;
            msg.HasLatitude = true;
        }
        if (TryGetDouble(csv, headers, _mapping.Longitude, out var lon))
        {
            msg.Longitude = lon;
            msg.HasLongitude = true;
        }

        // Battery
        if (TryGetDouble(csv, headers, _mapping.BatteryLevel, out var soc))
        {
            msg.BatteryLevel = soc;
            msg.HasBatteryLevel = true;
        }
        if (TryGetDouble(csv, headers, _mapping.BatteryVoltage, out var volt))
        {
            msg.BatteryVoltage = volt;
            msg.HasBatteryVoltage = true;
        }
        if (TryGetDouble(csv, headers, _mapping.BatteryCurrent, out var curr))
        {
            msg.BatteryCurrent = curr;
            msg.HasBatteryCurrent = true;
        }

        // Engine
        if (TryGetDouble(csv, headers, _mapping.EngineRpm, out var rpm))
        {
            msg.EngineRpm = rpm;
            msg.HasEngineRpm = true;
        }
        if (TryGetDouble(csv, headers, _mapping.EngineLoad, out var load))
        {
            msg.EngineLoad = load;
            msg.HasEngineLoad = true;
        }
        if (TryGetDouble(csv, headers, _mapping.FuelRate, out var fuel))
        {
            msg.FuelRate = fuel;
            msg.HasFuelRate = true;
        }

        // Energy
        if (TryGetDouble(csv, headers, _mapping.EnergyConsumption, out var energy))
        {
            msg.EnergyConsumption = energy;
            msg.HasEnergyConsumption = true;
        }

        // Environment
        if (TryGetDouble(csv, headers, _mapping.Temperature, out var temp))
        {
            msg.Temperature = temp;
            msg.HasTemperature = true;
        }
        if (TryGetDouble(csv, headers, _mapping.Distance, out var dist))
        {
            msg.Distance = dist;
            msg.HasDistance = true;
        }

        // Kaggle Specific Telemetry
        if (TryGetDouble(csv, headers, _mapping.MassAirFlow, out var maf))
        {
            msg.MassAirFlow = maf;
            msg.HasMassAirFlow = true;
        }
        if (TryGetDouble(csv, headers, _mapping.AirConditioningPower, out var acPower))
        {
            msg.AirConditioningPower = acPower;
            msg.HasAirConditioningPower = true;
        }
        if (TryGetDouble(csv, headers, _mapping.HeaterPower, out var heater))
        {
            msg.HeaterPower = heater;
            msg.HasHeaterPower = true;
        }
        if (TryGetDouble(csv, headers, _mapping.Elevation, out var elev))
        {
            msg.Elevation = elev;
            msg.HasElevation = true;
        }
        if (TryGetDouble(csv, headers, _mapping.SpeedLimit, out var limit))
        {
            msg.SpeedLimit = limit;
            msg.HasSpeedLimit = true;
        }

        return msg;
    }

    private static string? GetString(CsvReader csv, string[] headers, string columnName)
    {
        if (!headers.Contains(columnName, StringComparer.OrdinalIgnoreCase)) return null;
        var value = csv.GetField(columnName);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool TryGetDouble(CsvReader csv, string[] headers, string columnName, out double result)
    {
        result = 0;
        if (!headers.Contains(columnName, StringComparer.OrdinalIgnoreCase)) return false;
        var raw = csv.GetField(columnName);
        return !string.IsNullOrWhiteSpace(raw) && double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private static string BuildRawJson(CsvReader csv, string[] headers)
    {
        var dict = headers.ToDictionary(h => h, h =>
        {
            try { return csv.GetField(h); }
            catch { return null; }
        });
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    private static void GenerateMockCsv(string csvPath)
    {
        var dir = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vehicle_ID,Trip_ID,Timestamp,Speed,Latitude,Longitude,SOC,RPM,Engine_Load,Fuel_Rate,Energy_Consumption,Battery_Voltage,Battery_Current,Outside_Air_Temp,Distance");

        var baseTime = DateTime.UtcNow.AddHours(-2);

        // Vehicle 1: Normal with speed & high temperature alert
        string vehicle1 = "VEH_TRK_001";
        string trip1 = "TRIP_101";
        double lat1 = 41.0082;
        double lon1 = 28.9784;
        double soc1 = 85.0;
        double dist1 = 120.5;

        for (int i = 0; i < 60; i++)
        {
            var time = baseTime.AddSeconds(i * 10);
            double speed = 50.0 + i * 1.5; // reaches 140 km/h (overspeed threshold > 130)
            if (i > 45) speed = 80.0;      // slows down

            double temp = 25.0 + i * 0.5;
            if (i >= 30 && i <= 35) temp = 108.0; // High Temperature alert
            if (i == 33 || i == 34) temp = 117.0; // Critical Temperature alert

            double rpm = 1500 + speed * 15;
            double engineLoad = 30 + (speed / 3);
            double fuelRate = 5 + (speed / 10);
            double energy = 180 + (speed * 1.2);

            soc1 -= 0.05;
            dist1 += (speed * 10 / 3600.0);
            lat1 += 0.0001;
            lon1 += 0.00015;

            sb.AppendLine(Invariant($"{vehicle1},{trip1},{time:yyyy-MM-ddTHH:mm:ssZ},{speed:F2},{lat1:F6},{lon1:F6},{soc1:F2},{rpm:F1},{engineLoad:F1},{fuelRate:F2},{energy:F2},400.0,2.5,{temp:F1},{dist1:F3}"));
        }

        // Vehicle 2: Low battery EV
        string vehicle2 = "VEH_EV_002";
        string trip2 = "TRIP_202";
        double lat2 = 40.9901;
        double lon2 = 29.0205;
        double soc2 = 22.0;
        double dist2 = 54.2;

        for (int i = 0; i < 40; i++)
        {
            var time = baseTime.AddSeconds(i * 15);
            double speed = 40.0;
            double temp = 24.5;
            double rpm = 0.0;
            double engineLoad = 15.0;
            double fuelRate = 0.0;
            double energy = 150.0;

            soc2 -= 0.35; // drops rapidly below 20 and 10 to trigger battery alerts
            dist2 += (speed * 15 / 3600.0);
            lat2 -= 0.00008;
            lon2 += 0.00012;

            sb.AppendLine(Invariant($"{vehicle2},{trip2},{time:yyyy-MM-ddTHH:mm:ssZ},{speed:F2},{lat2:F6},{lon2:F6},{soc2:F2},{rpm:F1},{engineLoad:F1},{fuelRate:F2},{energy:F2},360.0,-1.2,{temp:F1},{dist2:F3}"));
        }

        // Vehicle 3: Abnormal energy consumption
        string vehicle3 = "VEH_TRK_003";
        string trip3 = "TRIP_303";
        double lat3 = 41.0422;
        double lon3 = 29.0081;
        double soc3 = 70.0;
        double dist3 = 310.8;

        for (int i = 0; i < 20; i++)
        {
            var time = baseTime.AddSeconds(i * 30);
            double speed = 90.0;
            double temp = 30.0;
            double rpm = 2500;
            double engineLoad = 85.0;
            double fuelRate = 22.0;
            double energy = 550.0; // Abnormal energy consumption (>500)

            soc3 -= 0.2;
            dist3 += (speed * 30 / 3600.0);
            lat3 += 0.0002;
            lon3 -= 0.0001;

            sb.AppendLine(Invariant($"{vehicle3},{trip3},{time:yyyy-MM-ddTHH:mm:ssZ},{speed:F2},{lat3:F6},{lon3:F6},{soc3:F2},{rpm:F1},{engineLoad:F1},{fuelRate:F2},{energy:F2},390.0,4.8,{temp:F1},{dist3:F3}"));
        }

        File.WriteAllText(csvPath, sb.ToString(), System.Text.Encoding.UTF8);
    }
}
