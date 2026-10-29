namespace FunkoApiV._2.Config;

public static class FunkoConfig {
    public static IConfiguration Config { get; private set; } = null!;
    
    public static void Init(string[] args) {
        var archivoConfiguracion = (args.FirstOrDefault() ?? "Development").ToLowerInvariant() switch {
            "development" => "appsettings.Development.json",
            "production" => "appsettings.Production.json",
            var entorno => throw new ArgumentException($"Entorno de configuración no soportado: {entorno}.", nameof(args))
        };
        
        Config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(archivoConfiguracion, false, true)
            .Build();
    }

    public static string ApiName => 
        Config.GetValue<string>("ApiSettings:Name") 
        ?? throw new InvalidOperationException("Falta ApiSettings:Name en el archivo JSON.");
    
    public static string CacheName => 
        Config.GetValue<string>("Cache:Name") 
        ?? throw new InvalidOperationException("Falta ApiSettings:Name en el archivo JSON.");


    public static string RepositoryName => 
        Config.GetValue<string>("Repository:Name") 
        ?? throw new InvalidOperationException("Falta Repository:Name en el archivo JSON.");

    public static string DbConnection =>
        RepositoryName.Equals("SQLite", StringComparison.OrdinalIgnoreCase)
            ? $"Data Source={Path.Combine(DataFolder, "funkos.db")}"
            : Config.GetValue<string>("Repository:ConnectionString")
              ?? throw new InvalidOperationException("Falta Repository:ConnectionString en el archivo JSON de producción.");
    
    public static string DataFolder => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "data"
    );
}