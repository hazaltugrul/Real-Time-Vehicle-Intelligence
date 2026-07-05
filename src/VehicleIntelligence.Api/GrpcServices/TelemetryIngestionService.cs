using FluentValidation;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using VehicleIntelligence.Application.Events;
using VehicleIntelligence.Grpc;

namespace VehicleIntelligence.Api.GrpcServices;

/// <summary>
/// gRPC server-side handler for real-time telemetry streaming.
/// Validates each message, publishes valid ones to RabbitMQ, and tracks rejected records.
/// </summary>
public sealed class TelemetryIngestionService : TelemetryIngestion.TelemetryIngestionBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IValidator<TelemetryReceivedEvent> _validator;
    private readonly ILogger<TelemetryIngestionService> _logger;

    public TelemetryIngestionService(
        IPublishEndpoint publishEndpoint,
        IValidator<TelemetryReceivedEvent> validator,
        ILogger<TelemetryIngestionService> logger)
    {
        _publishEndpoint = publishEndpoint;
        _validator = validator;
        _logger = logger;
    }

    public override async Task<TelemetryStreamResponse> StreamTelemetry(
        IAsyncStreamReader<TelemetryMessage> requestStream,
        ServerCallContext context)
    {
        var acceptedCount = 0;
        var rejectedCount = 0;

        await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            var telemetryEvent = MapToEvent(message);

            var validationResult = await _validator.ValidateAsync(telemetryEvent, context.CancellationToken);

            if (!validationResult.IsValid)
            {
                rejectedCount++;
                _logger.LogWarning(
                    "Invalid telemetry rejected for vehicle {VehicleId}. Errors: {Errors}",
                    message.VehicleId,
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                continue;
            }

            await _publishEndpoint.Publish(telemetryEvent, context.CancellationToken);
            acceptedCount++;

            _logger.LogDebug(
                "Telemetry accepted for vehicle {VehicleId} at {Timestamp}. Speed={Speed}",
                message.VehicleId, message.Timestamp, message.Speed);
        }

        _logger.LogInformation(
            "Telemetry stream completed. Accepted={Accepted}, Rejected={Rejected}",
            acceptedCount, rejectedCount);

        return new TelemetryStreamResponse
        {
            AcceptedCount = acceptedCount,
            RejectedCount = rejectedCount,
            Message = $"Stream processed: {acceptedCount} accepted, {rejectedCount} rejected."
        };
    }

    private static TelemetryReceivedEvent MapToEvent(TelemetryMessage msg)
    {
        var parsed = DateTime.TryParse(
            msg.Timestamp,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsedTimestamp);

        return new TelemetryReceivedEvent
        {
            VehicleExternalId = msg.VehicleId,
            TripId = string.IsNullOrWhiteSpace(msg.TripId) ? null : msg.TripId,
            Timestamp = parsed ? parsedTimestamp : DateTime.UtcNow,
            Speed = msg.HasSpeed ? msg.Speed : null,
            Latitude = msg.HasLatitude ? msg.Latitude : null,
            Longitude = msg.HasLongitude ? msg.Longitude : null,
            BatteryLevel = msg.HasBatteryLevel ? msg.BatteryLevel : null,
            BatteryVoltage = msg.HasBatteryVoltage ? msg.BatteryVoltage : null,
            BatteryCurrent = msg.HasBatteryCurrent ? msg.BatteryCurrent : null,
            EngineRpm = msg.HasEngineRpm ? msg.EngineRpm : null,
            EngineLoad = msg.HasEngineLoad ? msg.EngineLoad : null,
            FuelRate = msg.HasFuelRate ? msg.FuelRate : null,
            EnergyConsumption = msg.HasEnergyConsumption ? msg.EnergyConsumption : null,
            Temperature = msg.HasTemperature ? msg.Temperature : null,
            Distance = msg.HasDistance ? msg.Distance : null,
            MassAirFlow = msg.HasMassAirFlow ? msg.MassAirFlow : null,
            AirConditioningPower = msg.HasAirConditioningPower ? msg.AirConditioningPower : null,
            HeaterPower = msg.HasHeaterPower ? msg.HeaterPower : null,
            Elevation = msg.HasElevation ? msg.Elevation : null,
            SpeedLimit = msg.HasSpeedLimit ? msg.SpeedLimit : null,
            RawPayloadJson = string.IsNullOrWhiteSpace(msg.RawPayloadJson) ? null : msg.RawPayloadJson
        };
    }
}
