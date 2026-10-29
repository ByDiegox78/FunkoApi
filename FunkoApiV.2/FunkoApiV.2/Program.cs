using System.Text;
using FunkoApiMongo.Cache;
using FunkoApiV._2.Config;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FunkoApiV._2;

public class Program {
    
    private const string Titulo = "FunkoApi V.2 - Backend Server";

    public static async Task Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = Titulo;
        
        try {

            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment()) {
                builder.Services.AddMemoryCache();
                builder.Services.AddSingleton<ICacheService, MemCacheService>();
            } else {
                builder.Services.AddStackExchangeRedisCache(op => {
                    op.Configuration = builder.Configuration.GetConnectionString("Redis");
                });
                builder.Services.AddSingleton<ICacheService, RedisService>();
            }
            FunkoConfig.Init(args);

            builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));
            
            builder.Services.AddControllers();

            if (FunkoConfig.RepositoryName.Equals("SQLite", StringComparison.OrdinalIgnoreCase)) {
                Directory.CreateDirectory(FunkoConfig.DataFolder);
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(FunkoConfig.DbConnection));
            } else {
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(FunkoConfig.DbConnection));
            }

            // 3. Registrar dependencias con Scrutor
            builder.Services.AddDependencies();

            var app = builder.Build();

            // 4. Inicializar / Crear la base de datos y Seed Data al arrancar
            await InitializeDatabaseAsync(app.Services);

            MostrarCabecera();

            app.UseHttpsRedirection();
            app.MapControllers();

            await app.RunAsync();
        } catch (Exception ex) {
            Console.WriteLine($"ERROR AL INICIAR LA API: {ex}");
            throw;
        }

    }

    /// <summary>
    /// Crea la base de datos y aplica el Seed Data automáticamente si no existe.
    /// </summary>
    private static async Task InitializeDatabaseAsync(IServiceProvider provider) {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Muestra la cabecera informativa por consola al arrancar la API.
    /// </summary>
    private static void MostrarCabecera() {
        Console.WriteLine();
        Console.WriteLine("┌────────────────────────────────────────────────┐");
        Console.WriteLine("│       FunkoApi V.2 - Servidor Web Activo       │");
        Console.WriteLine("└────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine($" Entorno / API:  {FunkoConfig.ApiName}");
        Console.WriteLine($" Repositorio:    {FunkoConfig.RepositoryName}");
        Console.WriteLine($" Ruta Datos:     {FunkoConfig.DataFolder}");
        Console.WriteLine($" Cache Name:     {FunkoConfig.CacheName}");

        Console.WriteLine();
    }
}