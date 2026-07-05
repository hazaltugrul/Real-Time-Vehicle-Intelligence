using Mapster;
using Microsoft.AspNetCore.Mvc;
using VehicleIntelligence.Application.DTOs;
using VehicleIntelligence.Domain.Enums;
using VehicleIntelligence.Domain.Interfaces;

namespace VehicleIntelligence.Api.Controllers;

/// <summary>
/// Alert management endpoints — list, detail, and resolve alerts.
/// </summary>
[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertRepository _alertRepository;

    public AlertsController(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    /// <summary>Returns a paged list of alerts with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? alertType = null,
        [FromQuery] bool? isResolved = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 20;

        AlertSeverity? parsedSeverity = null;
        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<AlertSeverity>(severity, true, out var sev))
            parsedSeverity = sev;

        AlertType? parsedAlertType = null;
        if (!string.IsNullOrWhiteSpace(alertType) && Enum.TryParse<AlertType>(alertType, true, out var aType))
            parsedAlertType = aType;

        var (items, totalCount) = await _alertRepository.GetPagedAsync(
            vehicleId, parsedSeverity, parsedAlertType, isResolved, page, pageSize, cancellationToken);

        var dtos = items.Select(a => a.Adapt<AlertDto>()).ToList();
        return Ok(new PagedResult<AlertDto>(dtos, totalCount, page, pageSize));
    }

    /// <summary>Returns details of a specific alert.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);
        if (alert is null) return NotFound(new { message = $"Alert {id} not found." });

        return Ok(alert.Adapt<AlertDto>());
    }

    /// <summary>Marks an alert as resolved.</summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);
        if (alert is null) return NotFound(new { message = $"Alert {id} not found." });

        if (alert.IsResolved)
            return Conflict(new { message = $"Alert {id} is already resolved." });

        alert.Resolve();
        await _alertRepository.UpdateAsync(alert, cancellationToken);

        return Ok(alert.Adapt<AlertDto>());
    }
}
