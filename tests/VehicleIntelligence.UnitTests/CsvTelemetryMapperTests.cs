using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleIntelligence.Simulator;
using Xunit;

namespace VehicleIntelligence.UnitTests;

public sealed class CsvTelemetryMapperTests : IDisposable
{
    private readonly string _tempFile;

    public CsvTelemetryMapperTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void ReadMessages_ValidCsvFile_ParsesCorrectly()
    {
        // Arrange
        var csvContent = "Vehicle_ID,Trip_ID,Timestamp,Speed,Latitude,Longitude,SOC,RPM,Engine_Load,Fuel_Rate,Energy_Consumption,Battery_Voltage,Battery_Current,Outside_Air_Temp,Distance\n" +
                         "VEH-001,TRIP-101,2026-07-05T12:00:00Z,90.5,41.0082,28.9784,80.0,2200,45.0,8.5,250.0,400.0,2.1,25.0,120.4\n";
        
        File.WriteAllText(_tempFile, csvContent);

        var mappingOptions = new CsvMappingOptions
        {
            VehicleId = "Vehicle_ID",
            TripId = "Trip_ID",
            Timestamp = "Timestamp",
            Speed = "Speed",
            Latitude = "Latitude",
            Longitude = "Longitude",
            BatteryLevel = "SOC",
            EngineRpm = "RPM",
            EngineLoad = "Engine_Load",
            FuelRate = "Fuel_Rate",
            EnergyConsumption = "Energy_Consumption",
            BatteryVoltage = "Battery_Voltage",
            BatteryCurrent = "Battery_Current",
            Temperature = "Outside_Air_Temp",
            Distance = "Distance"
        };

        var options = Options.Create(mappingOptions);
        var sut = new CsvTelemetryMapper(options, NullLogger<CsvTelemetryMapper>.Instance);

        // Act
        var messages = sut.ReadMessages(_tempFile, 10).ToList();

        // Assert
        var msg = Assert.Single(messages);
        Assert.Equal("VEH-001", msg.VehicleId);
        Assert.Equal("TRIP-101", msg.TripId);
        Assert.True(msg.HasSpeed);
        Assert.Equal(90.5, msg.Speed);
        Assert.True(msg.HasLatitude);
        Assert.Equal(41.0082, msg.Latitude);
        Assert.True(msg.HasLongitude);
        Assert.Equal(28.9784, msg.Longitude);
        Assert.True(msg.HasBatteryLevel);
        Assert.Equal(80.0, msg.BatteryLevel);
        Assert.True(msg.HasTemperature);
        Assert.Equal(25.0, msg.Temperature);
        Assert.True(msg.HasDistance);
        Assert.Equal(120.4, msg.Distance);
    }
}