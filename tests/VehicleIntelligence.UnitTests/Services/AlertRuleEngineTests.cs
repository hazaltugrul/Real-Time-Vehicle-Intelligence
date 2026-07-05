using System;
using System.Linq;
using VehicleIntelligence.Application.Services;
using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;
using Xunit;

namespace VehicleIntelligence.UnitTests.Services;

public sealed class AlertRuleEngineTests
{
    private readonly AlertRuleEngine _sut = new();

    private static TelemetryRecord CreateRecord(
        double? speed = 80,
        double? temperature = 25,
        double? batteryLevel = 80,
        double? energyConsumption = 200,
        double? latitude = 41.0,
        double? longitude = 29.0,
        double? speedLimit = null) =>
        TelemetryRecord.Create(
            vehicleId: Guid.NewGuid(),
            timestamp: DateTime.UtcNow,
            speed: speed,
            latitude: latitude,
            longitude: longitude,
            batteryLevel: batteryLevel,
            temperature: temperature,
            energyConsumption: energyConsumption,
            speedLimit: speedLimit);

    [Fact]
    public void Evaluate_NormalConditions_ReturnsNoAlerts()
    {
        var record = CreateRecord();
        var alerts = _sut.Evaluate(record, 0).ToList();

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_Overspeed_ReturnsHighAlert()
    {
        var record = CreateRecord(speed: 135);
        var alerts = _sut.Evaluate(record, 30).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.Overspeed, alert.AlertType);
        Assert.Equal(AlertSeverity.High, alert.Severity);
    }

    [Fact]
    public void Evaluate_OverspeedWithDynamicLimit_ReturnsHighAlert()
    {
        var record = CreateRecord(speed: 95, speedLimit: 90);
        var alerts = _sut.Evaluate(record, 30).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.Overspeed, alert.AlertType);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Contains($"limit of {90.0:F1} km/h", alert.Message);
    }

    [Fact]
    public void Evaluate_SpeedUnderDynamicLimit_ReturnsNoAlerts()
    {
        var record = CreateRecord(speed: 135, speedLimit: 140);
        var alerts = _sut.Evaluate(record, 0).ToList();

        Assert.Empty(alerts);
    }

    [Fact]
    public void Evaluate_HighTemperature_ReturnsHighAlert()
    {
        var record = CreateRecord(temperature: 105);
        var alerts = _sut.Evaluate(record, 15).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.HighTemperature, alert.AlertType);
        Assert.Equal(AlertSeverity.High, alert.Severity);
    }

    [Fact]
    public void Evaluate_CriticalTemperature_ReturnsCriticalAlert()
    {
        var record = CreateRecord(temperature: 118);
        var alerts = _sut.Evaluate(record, 30).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.HighTemperature, alert.AlertType);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void Evaluate_LowBattery_ReturnsMediumAlert()
    {
        var record = CreateRecord(batteryLevel: 15);
        var alerts = _sut.Evaluate(record, 10).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.LowBattery, alert.AlertType);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);
    }

    [Fact]
    public void Evaluate_CriticallyLowBattery_ReturnsHighAlert()
    {
        var record = CreateRecord(batteryLevel: 8);
        var alerts = _sut.Evaluate(record, 20).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.LowBattery, alert.AlertType);
        Assert.Equal(AlertSeverity.High, alert.Severity);
    }

    [Fact]
    public void Evaluate_AbnormalEnergy_ReturnsMediumAlert()
    {
        var record = CreateRecord(energyConsumption: 550);
        var alerts = _sut.Evaluate(record, 5).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.AbnormalEnergyConsumption, alert.AlertType);
        Assert.Equal(AlertSeverity.Medium, alert.Severity);
    }

    [Fact]
    public void Evaluate_InvalidTelemetry_ReturnsLowAlert()
    {
        var record = CreateRecord(speed: -5);
        var alerts = _sut.Evaluate(record, 15).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.InvalidTelemetry, alert.AlertType);
        Assert.Equal(AlertSeverity.Low, alert.Severity);
    }
}
