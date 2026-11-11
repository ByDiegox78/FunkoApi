using DnsClient.Internal;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FunkoApiV._2.Seeders;

public static class IdentitySeeder {
    public static async Task SeedAsync(IServiceProvider scopeServices, ILogger logger) {
        await EnsureRolesAsync(scopeServices, logger);

        var roleManager = scopeServices.GetRequiredService<RoleManager<IdentityRole<long>>>();
        var userManager = scopeServices.GetRequiredService<UserManager<AppUser>>();

        // ── Usuario ADMIN ──
        const string adminEmail = "admin@admin.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null) {
            var admin = new AppUser {
                UserName = adminEmail,
                Email = adminEmail,
                Nombre = "Admin",
                Apellido = "Sistema",
                Createdt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded) {
                await userManager.AddToRoleAsync(admin, "ADMIN");
                logger.LogInformation("Usuario admin creado: {Email}", adminEmail);
            } else {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Error al crear admin: {Errors}", errors);
            }
        }

        // ── Usuario USER ──
        const string userEmail = "user@user.com";
        if (await userManager.FindByEmailAsync(userEmail) is null) {
            var user = new AppUser {
                UserName = userEmail,
                Email = userEmail,
                Nombre = "Usuario",
                Apellido = "Regular",
                Createdt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, "User123!");
            if (result.Succeeded) {
                await userManager.AddToRoleAsync(user, "USER");
                logger.LogInformation("Usuario regular creado: {Email}", userEmail);
            } else {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Error al crear usuario: {Errors}", errors);
            }
        }
    }

    public static async Task EnsureRolesAsync(IServiceProvider scopeServices, ILogger logger) {
        var roleManager = scopeServices.GetRequiredService<RoleManager<IdentityRole<long>>>();
        string[] roles = ["ADMIN", "USER"];
        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role)) {
                var result = await roleManager.CreateAsync(new IdentityRole<long>(role));
                if (!result.Succeeded) {
                    var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"No se pudo crear el rol '{role}': {errors}");
                }
                logger.LogInformation("Rol '{Role}' creado", role);
            }
        }
    }

    public static async Task EnsureProductionAdminAsync(
        IServiceProvider scopeServices,
        IConfiguration configuration,
        ILogger logger) {
        await EnsureRolesAsync(scopeServices, logger);

        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        var nombre = configuration["BootstrapAdmin:FirstName"];
        var apellido = configuration["BootstrapAdmin:LastName"];
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido)) {
            throw new InvalidOperationException(
                "Production requiere BootstrapAdmin:Email, Password, FirstName y LastName para aprovisionar el administrador.");
        }

        var userManager = scopeServices.GetRequiredService<UserManager<AppUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null) {
            admin = new AppUser {
                UserName = email,
                Email = email,
                Nombre = nombre,
                Apellido = apellido,
                Createdt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded) {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"No se pudo crear el administrador de Production: {errors}");
            }
            var roleResult = await userManager.AddToRoleAsync(admin, "ADMIN");
            if (!roleResult.Succeeded) {
                var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"No se pudo asignar el rol ADMIN: {errors}");
            }
            logger.LogInformation("Administrador inicial de Production creado para {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(admin, "ADMIN")) {
            throw new InvalidOperationException(
                "El email BootstrapAdmin:Email ya pertenece a un usuario sin rol ADMIN. Usa una dirección exclusiva de administrador.");
        }
    }
}