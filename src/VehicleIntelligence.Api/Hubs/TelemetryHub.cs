using Microsoft.AspNetCore.SignalR;

namespace VehicleIntelligence.Api.Hubs;

/// <summary>
/// SignalR Hub for streaming real-time vehicle telemetry and alerts to the web dashboard.
/// </summary>
public sealed class TelemetryHub : Hub
{
    // Real-time broadcasts will be dispatched from consumers/controllers using IHubContext.
}
