using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using VehicleIntelligence.Simulator;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<SimulatorOptions>(ctx.Configuration.GetSection(SimulatorOptions.SectionName));
        services.Configure<CsvMappingOptions>(ctx.Configuration.GetSection(CsvMappingOptions.SectionName));
        services.AddSingleton<CsvTelemetryMapper>();
        services.AddSingleton<SimulatorService>();
    })
    .Build();

Console.Clear();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   Vehicle Intelligence Platform — Telemetry Simulator ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("Shutdown requested...");
    cts.Cancel();
};

try
{
    var simulator = host.Services.GetRequiredService<SimulatorService>();
    await simulator.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Simulator stopped by user.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Simulator encountered a fatal error.");
}
finally
{
    Log.CloseAndFlush();
}
