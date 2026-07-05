using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Infrastructure.Cache;
using VehicleIntelligence.Infrastructure.Persistence;
using VehicleIntelligence.Infrastructure.Repositories;

namespace VehicleIntelligence.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Called from the API and Worker host builders.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via EF Core
        services.AddDbContext<VehicleIntelligenceDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null));
        });

        // Redis
        services.Configure<RedisOptions>(options =>
        {
            configuration.GetSection(RedisOptions.SectionName).Bind(options);
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"]
                ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Repositories
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // Cache
        services.AddScoped<IVehicleStatusCache, RedisVehicleStatusCache>();

        return services;
    }
}
