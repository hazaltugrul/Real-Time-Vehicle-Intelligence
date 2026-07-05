using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VehicleIntelligence.Domain.Interfaces;
using VehicleIntelligence.Domain.ValueObjects;

namespace VehicleIntelligence.Infrastructure.Cache;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public int LatestStatusTtlMinutes { get; set; } = 10;
    public int DashboardSummaryTtlSeconds { get; set; } = 30;
}

internal sealed class RedisVehicleStatusCache : IVehicleStatusCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisVehicleStatusCache> _logger;
    private readonly TimeSpan _ttl;

    private static string BuildKey(Guid vehicleId) => $"vehicle:latest:{vehicleId}";

    public RedisVehicleStatusCache(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options,
        ILogger<RedisVehicleStatusCache> logger)
    {
        _redis = redis;
        _logger = logger;
        _ttl = TimeSpan.FromMinutes(options.Value.LatestStatusTtlMinutes);
    }

    public async Task<VehicleLatestStatus?> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(BuildKey(vehicleId));

            if (value.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<VehicleLatestStatus>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for vehicle {VehicleId}. Falling back to DB.", vehicleId);
            return null;
        }
    }

    public async Task SetAsync(VehicleLatestStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(status);
            await db.StringSetAsync(BuildKey(status.VehicleId), json, _ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for vehicle {VehicleId}.", status.VehicleId);
        }
    }

    public async Task RemoveAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(BuildKey(vehicleId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for vehicle {VehicleId}.", vehicleId);
        }
    }
}
