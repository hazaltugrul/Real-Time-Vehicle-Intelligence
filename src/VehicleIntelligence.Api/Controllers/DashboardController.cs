using Microsoft.AspNetCore.Mvc;
using VehicleIntelligence.Application.DTOs;
using VehicleIntelligence.Domain.Interfaces;

namespace VehicleIntelligence.Api.Controllers;

/// <summary>
/// Dashboard summary endpoint — provides at-a-glance platform metrics.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IAlertRepository _alertRepository;

    // Online threshold: vehicle seen within last 5 minutes
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);

    public DashboardController(
        IVehicleRepository vehicleRepository,
        ITelemetryRepository telemetryRepository,
        IAlertRepository alertRepository)
    {
        _vehicleRepository = vehicleRepository;
        _telemetryRepository = telemetryRepository;
        _alertRepository = alertRepository;
    }

    /// <summary>Returns a real-time summary of the entire platform.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var since24Hours = DateTime.UtcNow.AddHours(-24);

        var totalVehicles = await _vehicleRepository.GetTotalCountAsync(cancellationToken);
        var onlineVehicles = await _vehicleRepository.GetOnlineCountAsync(OnlineThreshold, cancellationToken);
        var telemetryLast24h = await _telemetryRepository.GetCountSinceAsync(since24Hours, cancellationToken);
        var openAlerts = await _alertRepository.GetOpenCountAsync(cancellationToken);
        var criticalAlerts = await _alertRepository.GetCriticalOpenCountAsync(cancellationToken);
        var avgRiskScore = await _telemetryRepository.GetAverageRiskScoreAsync(cancellationToken);

        return Ok(new DashboardSummaryDto(
            TotalVehicles: totalVehicles,
            OnlineVehicles: onlineVehicles,
            TelemetryLast24Hours: telemetryLast24h,
            OpenAlerts: openAlerts,
            CriticalAlerts: criticalAlerts,
            AverageRiskScore: Math.Round(avgRiskScore, 2)));
    }
}
