namespace FunkoApiMongo.Config;

public class CacheConfig {
    public const string CacheName = "Cache";
    
    public int ExpiryMinutes { get; set; } = 30;
    
    public int SlidingExpirationMinutes { get; set; } = 10;
    
    public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(ExpiryMinutes);
    public TimeSpan SlidingExpiration => TimeSpan.FromMinutes(SlidingExpirationMinutes);
}