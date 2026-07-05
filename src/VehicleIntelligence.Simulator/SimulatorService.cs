using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleIntelligence.Grpc;

namespace VehicleIntelligence.Simulator;

/// <summary>
/// Core simulator service that reads CSV and streams telemetry messages via gRPC client streaming.
/// </summary>
public sealed class SimulatorService
{
    private readonly SimulatorOptions _options;
    private readonly CsvTelemetryMapper _mapper;
    private readonly ILogger<SimulatorService> _logger;

    public SimulatorService(
        IOptions<SimulatorOptions> options,
        CsvTelemetryMapper mapper,
        ILogger<SimulatorService> logger)
    {
        _options = options.Value;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Vehicle Intelligence Simulator ===");
        _logger.LogInformation("CSV: {CsvPath}", _options.CsvPath);
        _logger.LogInformation("gRPC: {Endpoint}", _options.GrpcEndpoint);
        _logger.LogInformation("Delay: {Delay}ms | MaxRows: {MaxRows} | Loop: {Loop}",
            _options.DelayMilliseconds, _options.MaxRows, _options.Loop);

        do
        {
            await RunSinglePassAsync(cancellationToken);

            if (_options.Loop && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Loop enabled. Restarting stream...");
                await Task.Delay(2000, cancellationToken);
            }
        }
        while (_options.Loop && !cancellationToken.IsCancellationRequested);
    }

    private async Task RunSinglePassAsync(CancellationToken cancellationToken)
    {
        using var channel = GrpcChannel.ForAddress(_options.GrpcEndpoint, new GrpcChannelOptions
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }
        });

        var client = new TelemetryIngestion.TelemetryIngestionClient(channel);
        using var call = client.StreamTelemetry(cancellationToken: cancellationToken);

        int sent = 0;
        int failed = 0;

        var messages = _mapper.ReadMessages(_options.CsvPath, _options.MaxRows);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await call.RequestStream.WriteAsync(message, cancellationToken);
                sent++;

                if (sent % 100 == 0 || sent <= 10)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sent #{sent} | Vehicle: {message.VehicleId} | " +
                                      $"Speed: {(message.HasSpeed ? $"{message.Speed:F1} km/h" : "N/A")} | " +
                                      $"SOC: {(message.HasBatteryLevel ? $"{message.BatteryLevel:F1}%" : "N/A")} | " +
                                      $"Ts: {message.Timestamp}");
                    Console.ResetColor();
                }

                if (_options.DelayMilliseconds > 0)
                    await Task.Delay(_options.DelayMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed to send message #{Count}", sent + failed);
            }
        }

        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine($"  STREAM COMPLETE");
        Console.WriteLine($"  Accepted : {response.AcceptedCount}");
        Console.WriteLine($"  Rejected : {response.RejectedCount}");
        Console.WriteLine($"  Local Err: {failed}");
        Console.WriteLine($"  Server   : {response.Message}");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.ResetColor();
    }
}
