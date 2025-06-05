using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Play.Infrastructure.Common.Caching;

public interface IRedisCacheService
{
    // Define methods for Redis cache operations, e.g., Get, Set, Remove, etc.
    T GetData<T>(string key);
    void SetData<T>(string key, T data, TimeSpan? expiration = null);
    Task Remove(string key);
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public T? GetData<T>(string key)
    {
        try
        {
            var data = _cache.GetString(key);
            if (data is null)
            {
                return default(T);
            }
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            return default;
        }
    }

    public void SetData<T>(string key, T data, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            var jsonData = JsonSerializer.Serialize(data);
            _cache.SetString(key, jsonData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache for key: {Key}", key);
        }
    }

    public async Task Remove(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache for key: {Key}", key);
        }
    }
}

