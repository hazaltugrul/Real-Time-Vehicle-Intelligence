using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VehicleIntelligence.Api.Hubs;
using VehicleIntelligence.Application.Events;

namespace VehicleIntelligence.Api.Consumers;

/// <summary>
/// MassTransit consumer running inside the API project.
/// Listens to processed telemetry and alert events from RabbitMQ and broadcasts them via SignalR WebSockets.
/// </summary>
public sealed class TelemetryUpdateConsumer : 
    IConsumer<TelemetryProcessedEvent>,
    IConsumer<AlertTriggeredEvent>
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<TelemetryUpdateConsumer> _logger;

    public TelemetryUpdateConsumer(IHubContext<TelemetryHub> hubContext, ILogger<TelemetryUpdateConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TelemetryProcessedEvent> context)
    {
        var msg = context.Message;
        _logger.LogDebug("Broadcasting TelemetryProcessed for Vehicle {VehicleExternalId}", msg.VehicleExternalId);
        
        // Broadcast telemetry details to all connected SignalR clients
        await _hubContext.Clients.All.SendAsync("ReceiveTelemetryUpdate", msg, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<AlertTriggeredEvent> context)
    {
        var msg = context.Message;
        _logger.LogDebug("Broadcasting AlertTriggered for Vehicle {VehicleExternalId}", msg.VehicleExternalId);

        // Broadcast alert details to all connected SignalR clients
        await _hubContext.Clients.All.SendAsync("ReceiveAlert", msg, context.CancellationToken);
    }
}
