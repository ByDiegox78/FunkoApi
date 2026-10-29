using System.Text.Json;
using FunkoApiMongo.Config;
using FunkoApiV._2.Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace FunkoApiMongo.Cache;

public class RedisService(IDistributedCache cache, IOptions<CacheConfig> option) : ICacheService, ISingletonService {
    private readonly CacheConfig _config = option.Value;
    private readonly SemaphoreSlim _factory = new(1, 1);
    
    private readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken token = default) {
        var data = await cache.GetStringAsync(cacheKey, token);
        return string.IsNullOrEmpty(data) ? default : JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null, CancellationToken token = default) {
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _config.DefaultExpiration,
            SlidingExpiration = _config.SlidingExpiration
        };
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(value, _jsonOptions), entryOptions, token);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken token = default) {
        await cache.RemoveAsync(cacheKey, token);
    }

    public async Task<bool> ExistsAsync(string cacheKey, CancellationToken token = default) {
        var data = await cache.GetStringAsync(cacheKey, token);
        return !string.IsNullOrEmpty(data);
    }

    public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T?>> factory, CancellationToken token = default, TimeSpan? expiration = null) {
        var cached = await GetAsync<T>(cacheKey, token);
        if (cached is not null) return cached;

        await _factory.WaitAsync(token);
        try {
            cached = await GetAsync<T>(cacheKey, token);
            if (cached is not null) return cached;

            var value = await factory();
            if (value is not null)
                await SetAsync(cacheKey, value, expiration, token);
            return value;
        } finally {
            _factory.Release();
        }
    }
}