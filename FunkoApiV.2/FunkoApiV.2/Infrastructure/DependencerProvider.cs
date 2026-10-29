using FunkoApiMongo.Cache;
using FunkoApiMongo.Service;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Service;
using Microsoft.Extensions.DependencyInjection;

namespace FunkoApiV._2.Infrastructure;

public static class DependencerProvider {
    public static IServiceCollection AddDependencies(this IServiceCollection services) {
        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(c => c.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .WithTransientLifetime()
            .AddClasses(c => c.AssignableTo<IScopedService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo<ISingletonService>()
                .Where(type => type != typeof(MemCacheService) && type != typeof(RedisService)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );
        services.Decorate<IFunkoService, CachedFunkoService>();
        return services;
    }
}