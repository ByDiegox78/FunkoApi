using FunkoApiV._2.Realtime;
namespace FunkoApiV._2.Infrastructure;

public static class SignalRConfig {

    public static IServiceCollection AddRealtimeSignalR(
        this IServiceCollection services,
        bool isDevelopment) {
        services.AddSignalR(options => {
            options.EnableDetailedErrors = isDevelopment;
            options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        });
        return services;
    }

}
