using MassTransit;
using Serilog;
using Serilog.Events;
using VehicleIntelligence.Application;
using VehicleIntelligence.Infrastructure;
using VehicleIntelligence.Infrastructure.Persistence;
using VehicleIntelligence.Worker.Consumers;
using Microsoft.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/vehicle-worker-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Vehicle Intelligence Worker");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((ctx, services) =>
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices(ctx.Configuration);

            // MassTransit + RabbitMQ consumer
            services.AddMassTransit(x =>
            {
                x.AddConsumer<TelemetryReceivedEventConsumer>();

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var rabbitConfig = ctx.GetRequiredService<IConfiguration>().GetSection("RabbitMQ");
                    cfg.Host(rabbitConfig["Host"] ?? "localhost", h =>
                    {
                        h.Username(rabbitConfig["Username"] ?? "guest");
                        h.Password(rabbitConfig["Password"] ?? "guest");
                    });

                    cfg.ReceiveEndpoint("telemetry-received-queue", e =>
                    {
                        e.ConfigureConsumer<TelemetryReceivedEventConsumer>(ctx);
                        e.PrefetchCount = 10;
                        e.UseMessageRetry(r => r.Intervals(
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(15)));
                    });
                });
            });
        })
        .Build();

    // Apply DB migrations
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<VehicleIntelligenceDbContext>();
        Log.Information("Applying database migrations from Worker...");
        await db.Database.MigrateAsync();
    }

    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Worker terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
