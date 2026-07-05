using System;
using FluentValidation.TestHelper;
using VehicleIntelligence.Application.Events;
using VehicleIntelligence.Application.Validation;
using Xunit;

namespace VehicleIntelligence.UnitTests.Validation;

public sealed class TelemetryReceivedEventValidatorTests
{
    private readonly TelemetryReceivedEventValidator _validator = new();

    [Fact]
    public void Validate_ValidEvent_ReturnsNoErrors()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow,
            Speed = 90.0,
            Latitude = 41.0082,
            Longitude = 28.9784,
            BatteryLevel = 75.0,
            Temperature = 25.0
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyVehicleId_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "",
            Timestamp = DateTime.UtcNow
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.VehicleExternalId);
    }

    [Fact]
    public void Validate_NegativeSpeed_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow,
            Speed = -10.0
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Speed!.Value);
    }

    [Fact]
    public void Validate_InvalidLatitude_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow,
            Latitude = 95.0 // Max is 90
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Latitude!.Value);
    }

    [Fact]
    public void Validate_InvalidLongitude_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow,
            Longitude = -190.0 // Min is -180
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Longitude!.Value);
    }

    [Fact]
    public void Validate_InvalidBatteryLevel_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow,
            BatteryLevel = 105.0 // Max is 100
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BatteryLevel!.Value);
    }

    [Fact]
    public void Validate_FutureTimestamp_Fails()
    {
        var model = new TelemetryReceivedEvent
        {
            VehicleExternalId = "VEH_123",
            Timestamp = DateTime.UtcNow.AddHours(2)
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }
}
