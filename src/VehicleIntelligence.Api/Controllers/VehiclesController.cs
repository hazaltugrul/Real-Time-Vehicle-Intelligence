using Mapster;
using Microsoft.AspNetCore.Mvc;
using VehicleIntelligence.Application.DTOs;
using VehicleIntelligence.Domain.Interfaces;

namespace VehicleIntelligence.Api.Controllers;

/// <summary>
/// Vehicle management endpoints — list, detail, latest status, and telemetry history.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[Produces("application/json")]
public sealed class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IVehicleStatusCache _statusCache;

    public VehiclesController(
        IVehicleRepository vehicleRepository,
        ITelemetryRepository telemetryRepository,
        IVehicleStatusCache statusCache)
    {
        _vehicleRepository = vehicleRepository;
        _telemetryRepository = telemetryRepository;
        _statusCache = statusCache;
    }

    /// <summary>Returns a paged list of all registered vehicles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVehicles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (items, totalCount) = await _vehicleRepository.GetPagedAsync(page, pageSize, status, cancellationToken);
        var dtos = items.Select(v => v.Adapt<VehicleDto>()).ToList();

        return Ok(new PagedResult<VehicleDto>(dtos, totalCount, page, pageSize));
    }

    /// <summary>Returns detailed information about a specific vehicle.</summary>
    [HttpGet("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicleById(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null) return NotFound(new { message = $"Vehicle {vehicleId} not found." });

        return Ok(vehicle.Adapt<VehicleDto>());
    }

    /// <summary>
    /// Returns the latest known status for a vehicle.
    /// First checks Redis cache; falls back to most recent telemetry record in PostgreSQL.
    /// </summary>
    [HttpGet("{vehicleId:guid}/latest-status")]
    [ProducesResponseType(typeof(VehicleLatestStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestStatus(Guid vehicleId, CancellationToken cancellationToken)
    {
        // 1. Try Redis cache first
        var cached = await _statusCache.GetAsync(vehicleId, cancellationToken);
        if (cached is not null)
            return Ok(cached.Adapt<VehicleLatestStatusDto>());

        // 2. Fallback: query latest telemetry from DB
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null) return NotFound(new { message = $"Vehicle {vehicleId} not found." });

        var latestTelemetry = await _telemetryRepository.GetLatestByVehicleIdAsync(vehicleId, cancellationToken);
        if (latestTelemetry is null)
            return NotFound(new { message = $"No telemetry found for vehicle {vehicleId}." });

        var dto = new VehicleLatestStatusDto(
            vehicleId,
            latestTelemetry.Timestamp,
            latestTelemetry.Speed,
            latestTelemetry.Latitude,
            latestTelemetry.Longitude,
            latestTelemetry.BatteryLevel,
            latestTelemetry.Temperature,
            latestTelemetry.RiskScore,
            "Unknown",
            latestTelemetry.CreatedAt);

        return Ok(dto);
    }

    /// <summary>Returns paged telemetry history for a vehicle, optionally filtered by time range.</summary>
    [HttpGet("{vehicleId:guid}/telemetry-history")]
    [ProducesResponseType(typeof(PagedResult<TelemetryRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTelemetryHistory(
        Guid vehicleId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null) return NotFound(new { message = $"Vehicle {vehicleId} not found." });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 500) pageSize = 50;

        var (items, totalCount) = await _telemetryRepository.GetByVehicleIdPagedAsync(
            vehicleId, from, to, page, pageSize, cancellationToken);

        var dtos = items.Select(t => t.Adapt<TelemetryRecordDto>()).ToList();
        return Ok(new PagedResult<TelemetryRecordDto>(dtos, totalCount, page, pageSize));
    }
}
