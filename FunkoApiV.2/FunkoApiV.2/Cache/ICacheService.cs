namespace FunkoApiMongo.Cache;

public interface ICacheService {
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken token = default);
    Task SetAsync<T>(string cacheKey, T value,TimeSpan? expiration = null, CancellationToken token = default);
    Task RemoveAsync(string cacheKey, CancellationToken token = default);
    Task<bool> ExistsAsync(string cacheKey, CancellationToken token = default);
    Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T?>> factory, CancellationToken token = default,  TimeSpan? expiration = null);
}