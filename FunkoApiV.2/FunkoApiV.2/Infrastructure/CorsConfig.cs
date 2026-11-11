namespace FunkoApiV._2.Infrastructure;

public static class CorsConfig {
    public const string PolicyName = "FunkoCorsPolicy";
    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment) {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var defaultOrigins = new[] {
            "http://localhost:5500",
            "http://127.0.0.1:5500",
            "http://localhost:63342",
            "http://localhost:5798",
            "http://127.0.0.1:5798"
        };
        var allOrigins = (isDevelopment
                ? configuredOrigins.Concat(defaultOrigins)
                : configuredOrigins)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        services.AddCors(options => {
            options.AddPolicy(PolicyName, policy => {
                policy.WithOrigins(allOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Imprescindible para SignalR
            });
        });
        return services;
    }
    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app) {
        return app.UseCors(PolicyName);
    }
}