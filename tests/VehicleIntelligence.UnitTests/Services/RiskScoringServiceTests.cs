using VehicleIntelligence.Application.Services;
using VehicleIntelligence.Domain.Entities;
using Xunit;

namespace VehicleIntelligence.UnitTests.Services;

public sealed class RiskScoringServiceTests
{
    private readonly RiskScoringService _sut = new();

    private static TelemetryRecord CreateRecord(
        double? speed = 80,
        double? temperature = 25,
        double? batteryLevel = 80,
        double? latitude = 41.0,
        double? longitude = 29.0) =>
        TelemetryRecord.Create(
            vehicleId: Guid.NewGuid(),
            timestamp: DateTime.UtcNow,
            speed: speed,
            latitude: latitude,
            longitude: longitude,
            batteryLevel: batteryLevel,
            temperature: temperature);

    [Fact]
    public void Calculate_NormalConditions_ReturnsZeroRisk()
    {
        var record = CreateRecord(speed: 80, temperature: 25, batteryLevel: 80);
        var score = _sut.Calculate(record);
        Assert.Equal(0, score);
    }

    [Fact]
    public void Calculate_OverspeedAt160_Returns30SpeedRisk()
    {
        var record = CreateRecord(speed: 160);
        var score = _sut.Calculate(record);
        Assert.Equal(30, score, precision: 1);
    }

    [Fact]
    public void Calculate_CriticalTemperature_ReturnsMaxTempRisk()
    {
        var record = CreateRecord(temperature: 115);
        var score = _sut.Calculate(record);
        Assert.Equal(30, score, precision: 1);
    }

    [Fact]
    public void Calculate_CriticalBattery_ReturnsMaxBatteryRisk()
    {
        var record = CreateRecord(batteryLevel: 5);
        var score = _sut.Calculate(record);
        Assert.Equal(20, score, precision: 1);
    }

    [Fact]
    public void Calculate_MissingLocation_AddsValidityPenalty()
    {
        var record = CreateRecord(latitude: null, longitude: null);
        var score = _sut.Calculate(record);
        Assert.True(score >= 8);
    }

    [Fact]
    public void Calculate_AllCritical_ReturnsMaxScore()
    {
        var record = CreateRecord(speed: 200, temperature: 120, batteryLevel: 5, latitude: null, longitude: null);
        var score = _sut.Calculate(record);
        Assert.Equal(88, score, precision: 1);
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(115, 30)]
    [InlineData(107, 15)]
    public void Calculate_TemperatureRisk_IsProportional(double temperature, double expectedMin)
    {
        var record = CreateRecord(temperature: temperature);
        var score = _sut.Calculate(record);
        Assert.True(score >= expectedMin, $"Expected risk >= {expectedMin} for temp {temperature}, got {score}");
    }
}
