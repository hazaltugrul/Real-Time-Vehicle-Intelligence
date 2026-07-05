using FluentValidation;
using VehicleIntelligence.Application.Events;

namespace VehicleIntelligence.Application.Validation;

/// <summary>
/// FluentValidation validator for incoming telemetry events.
/// Invalid records are rejected at the gRPC ingestion layer.
/// </summary>
public sealed class TelemetryReceivedEventValidator : AbstractValidator<TelemetryReceivedEvent>
{
    public TelemetryReceivedEventValidator()
    {
        RuleFor(x => x.VehicleExternalId)
            .NotEmpty()
            .WithMessage("Vehicle ID must not be empty.")
            .MaximumLength(100);

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.")
            .Must(ts => ts.ToUniversalTime() > DateTime.UtcNow.AddYears(-1))
            .WithMessage("Timestamp is too old (more than 1 year ago).")
            .Must(ts => ts.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Timestamp cannot be in the future.");

        When(x => x.Speed.HasValue, () =>
        {
            RuleFor(x => x.Speed!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Speed cannot be negative.")
                .LessThanOrEqualTo(500)
                .WithMessage("Speed exceeds maximum allowed value (500 km/h).");
        });

        When(x => x.Latitude.HasValue, () =>
        {
            RuleFor(x => x.Latitude!.Value)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");
        });

        When(x => x.Longitude.HasValue, () =>
        {
            RuleFor(x => x.Longitude!.Value)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");
        });

        When(x => x.BatteryLevel.HasValue, () =>
        {
            RuleFor(x => x.BatteryLevel!.Value)
                .InclusiveBetween(0, 100)
                .WithMessage("Battery level (SOC) must be between 0 and 100 percent.");
        });

        When(x => x.Temperature.HasValue, () =>
        {
            RuleFor(x => x.Temperature!.Value)
                .InclusiveBetween(-60, 200)
                .WithMessage("Temperature is outside realistic bounds (-60°C to 200°C).");
        });

        When(x => x.EngineRpm.HasValue, () =>
        {
            RuleFor(x => x.EngineRpm!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Engine RPM cannot be negative.");
        });

        When(x => x.MassAirFlow.HasValue, () =>
        {
            RuleFor(x => x.MassAirFlow!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Mass Air Flow (MAF) cannot be negative.");
        });

        When(x => x.AirConditioningPower.HasValue, () =>
        {
            RuleFor(x => x.AirConditioningPower!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Air conditioning power cannot be negative.");
        });

        When(x => x.HeaterPower.HasValue, () =>
        {
            RuleFor(x => x.HeaterPower!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Heater power cannot be negative.");
        });

        When(x => x.SpeedLimit.HasValue, () =>
        {
            RuleFor(x => x.SpeedLimit!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Speed limit cannot be negative.");
        });
    }
}
