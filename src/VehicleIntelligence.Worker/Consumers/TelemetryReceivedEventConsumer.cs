using MassTransit;
using Microsoft.Extensions.Logging;
using VehicleIntelligence.Application.Events;
using VehicleIntelligence.Application.Services;
using VehicleIntelligence.Domain.Entities;
using VehicleIntelligence.Domain.Enums;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Domain.ValueObjects;

namespace VehicleIntelligence.Worker.Consumers;

/// <summary>
/// MassTransit consumer for TelemetryReceivedEvent.
/// Handles the full processing pipeline: vehicle upsert → persist → score → alert → cache → log.
/// </summary>
public sealed class TelemetryReceivedEventConsumer : IConsumer<TelemetryReceivedEvent>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IVehicleStatusCache _statusCache;
    private readonly IRiskScoringService _riskScoring;
    private readonly IAlertRuleEngine _alertRuleEngine;
    private readonly ILogger<TelemetryReceivedEventConsumer> _logger;

    public TelemetryReceivedEventConsumer(
        IVehicleRepository vehicleRepository,
        ITelemetryRepository telemetryRepository,
        IAlertRepository alertRepository,
        IVehicleStatusCache statusCache,
        IRiskScoringService riskScoring,
        IAlertRuleEngine alertRuleEngine,
        ILogger<TelemetryReceivedEventConsumer> logger)
    {
        _vehicleRepository = vehicleRepository;
        _telemetryRepository = telemetryRepository;
        _alertRepository = alertRepository;
        _statusCache = statusCache;
        _riskScoring = riskScoring;
        _alertRuleEngine = alertRuleEngine;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TelemetryReceivedEvent> context)
    {
        var evt = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation(
            "Consuming telemetry event {EventId} for vehicle {VehicleId} at {Timestamp}",
            evt.EventId, evt.VehicleExternalId, evt.Timestamp);

        // ── Step 1: Get or create vehicle ──────────────────────
        var vehicle = await _vehicleRepository.GetByExternalIdAsync(evt.VehicleExternalId, cancellationToken);
        if (vehicle is null)
        {
            vehicle = Vehicle.Create(evt.VehicleExternalId);
            vehicle = await _vehicleRepository.AddAsync(vehicle, cancellationToken);
            _logger.LogInformation("New vehicle registered: {VehicleId} (External: {ExternalId})",
                vehicle.Id, evt.VehicleExternalId);
        }

        // ── Step 2: Create and persist TelemetryRecord ──────────
        var record = TelemetryRecord.Create(
            vehicleId: vehicle.Id,
            timestamp: evt.Timestamp,
            tripId: evt.TripId,
            speed: evt.Speed,
            latitude: evt.Latitude,
            longitude: evt.Longitude,
            distance: evt.Distance,
            batteryLevel: evt.BatteryLevel,
            batteryVoltage: evt.BatteryVoltage,
            batteryCurrent: evt.BatteryCurrent,
            engineRpm: evt.EngineRpm,
            engineLoad: evt.EngineLoad,
            fuelRate: evt.FuelRate,
            energyConsumption: evt.EnergyConsumption,
            temperature: evt.Temperature,
            massAirFlow: evt.MassAirFlow,
            airConditioningPower: evt.AirConditioningPower,
            heaterPower: evt.HeaterPower,
            elevation: evt.Elevation,
            speedLimit: evt.SpeedLimit,
            rawPayloadJson: evt.RawPayloadJson);

        // ── Step 3: Calculate risk score ────────────────────────
        var riskScore = _riskScoring.Calculate(record);
        record.SetRiskScore(riskScore);

        record = await _telemetryRepository.AddAsync(record, cancellationToken);
        _logger.LogDebug("Telemetry persisted: RecordId={RecordId}, RiskScore={RiskScore:F2}",
            record.Id, riskScore);

        // ── Step 4: Evaluate alert rules ────────────────────────
        var alerts = _alertRuleEngine.Evaluate(record, riskScore).ToList();
        foreach (var alert in alerts)
        {
            await _alertRepository.AddAsync(alert, cancellationToken);
            _logger.LogWarning(
                "Alert generated: Type={AlertType}, Severity={Severity}, Vehicle={VehicleId}, RiskScore={RiskScore:F2}",
                alert.AlertType, alert.Severity, vehicle.Id, riskScore);
        }

        // ── Step 5: Update Redis latest status ──────────────────
        var connectionStatus = DetermineConnectionStatus(evt.Timestamp);
        var latestStatus = VehicleLatestStatus.FromTelemetry(
            vehicleId: vehicle.Id,
            timestamp: record.Timestamp,
            speed: record.Speed,
            latitude: record.Latitude,
            longitude: record.Longitude,
            batteryLevel: record.BatteryLevel,
            temperature: record.Temperature,
            riskScore: riskScore,
            connectionStatus: connectionStatus);

        await _statusCache.SetAsync(latestStatus, cancellationToken);
        _logger.LogDebug("Redis cache updated for vehicle {VehicleId}", vehicle.Id);

        // ── Step 6: Update vehicle LastSeenAt ──────────────────
        vehicle.UpdateLastSeen(evt.Timestamp);
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);

        _logger.LogInformation(
            "Telemetry processing complete: Vehicle={VehicleId}, Alerts={AlertCount}, RiskScore={RiskScore:F2}",
            vehicle.Id, alerts.Count, riskScore);
    }

    private static ConnectionStatus DetermineConnectionStatus(DateTime timestamp)
    {
        var age = DateTime.UtcNow - timestamp;
        return age switch
        {
            { TotalMinutes: <= 2 } => ConnectionStatus.Online,
            { TotalMinutes: <= 10 } => ConnectionStatus.Delayed,
            _ => ConnectionStatus.Offline
        };
    }
}
