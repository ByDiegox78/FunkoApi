using FunkoApiMongo.Config;
using FunkoApiV._2.Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;

namespace FunkoApiMongo.Cache;

public class MemCacheService(IMemoryCache cache, IOptions<CacheConfig> option, ILogger<MemCacheService> logger) : ICacheService, ISingletonService {
    private readonly CacheConfig _config = option.Value;
    private readonly SemaphoreSlim _factory = new(1, 1);
    
    
    
    public Task<T?> GetAsync<T>(string cacheKey, CancellationToken token = default) {
        logger.LogInformation($"Getting {cacheKey}");
        cache.TryGetValue(cacheKey, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null, CancellationToken token = default) {
        logger.LogInformation($"Setting {cacheKey} to {value}");
        var cacheOp = new MemoryCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = expiration ?? _config.DefaultExpiration,
            SlidingExpiration = _config.SlidingExpiration
        };
        cache.Set(cacheKey, value, cacheOp);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string cacheKey, CancellationToken token = default) {
        logger.LogInformation($"Removing {cacheKey}");
        cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string cacheKey, CancellationToken token = default) {
        logger.LogInformation($"Checking {cacheKey}");
        return Task.FromResult(cache.TryGetValue(cacheKey, out _));
    }

    public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T?>> factory, CancellationToken token = default, TimeSpan? expiration = null) {
        logger.LogInformation($"Getting {cacheKey}");
        if (cache.TryGetValue(cacheKey, out T? value)) 
            return value;
        await _factory.WaitAsync(token);

        try {
            if (cache.TryGetValue(cacheKey, out T? cached))
                return cached;
            var val = await factory();
            if (val is not null) 
                await SetAsync(cacheKey, val, expiration, token);
            return val;
        } finally {
            _factory.Release();
        }
        
    }
}