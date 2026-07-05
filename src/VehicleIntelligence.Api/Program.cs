using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Mapster;
using VehicleIntelligence.Api.GrpcServices;
using VehicleIntelligence.Api.Middleware;
using VehicleIntelligence.Application;
using VehicleIntelligence.Application.Events;
using VehicleIntelligence.Infrastructure;
using VehicleIntelligence.Infrastructure.Persistence;

// ─────────────────────────────────────────────
// Configure Serilog before the host starts
// ─────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/vehicle-api-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Vehicle Intelligence API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ─────────────────────────────────────────────
    // Services
    // ─────────────────────────────────────────────
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // MassTransit + RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
            cfg.Host(rabbitConfig["Host"] ?? "localhost", h =>
            {
                h.Username(rabbitConfig["Username"] ?? "guest");
                h.Password(rabbitConfig["Password"] ?? "guest");
            });
            cfg.ConfigureEndpoints(ctx);
        });
    });

    // gRPC
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });
    builder.Services.AddGrpcReflection();

    // Controllers + Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Vehicle Intelligence API",
            Version = "v1",
            Description = "Real-Time Vehicle Intelligence Platform — REST API for vehicle telemetry, alerts, and dashboard metrics."
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty, name: "postgresql")
        .AddRedis(
            builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379",
            name: "redis")
        .AddRabbitMQ(
            rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:Username"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:Host"]}:5672",
            name: "rabbitmq");

    // Mapster type adapters
    builder.Services.AddMapster();

    var app = builder.Build();

    // ─────────────────────────────────────────────
    // Middleware Pipeline
    // ─────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle Intelligence API v1");
            c.RoutePrefix = string.Empty; // Swagger at root
        });
        app.MapGrpcReflectionService();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.MapControllers();
    app.MapGrpcService<TelemetryIngestionService>();
    app.MapHealthChecks("/health");

    // ─────────────────────────────────────────────
    // Database Migration on startup
    // ─────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<VehicleIntelligenceDbContext>();
        Log.Information("Applying database migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
