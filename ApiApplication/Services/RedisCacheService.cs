using StackExchange.Redis;
using System.Text.Json;

namespace ApiApplication.Services;

/// <summary>
/// Redis implementation of the cache service
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultExpirationTime;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger, IConfiguration configuration)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;

        // Default to 1 hour if not specified in configuration
        _defaultExpirationTime = TimeSpan.FromMinutes(
            configuration.GetValue<double>("Redis:DefaultExpirationMinutes", 60));
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from Redis cache for key {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(
                key,
                serializedValue,
                expiry: expirationTime ?? _defaultExpirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data to Redis cache for key {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing data from Redis cache for key {Key}", key);
        }
    }
}
