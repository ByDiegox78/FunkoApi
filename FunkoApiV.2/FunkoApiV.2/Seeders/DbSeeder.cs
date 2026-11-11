using FunkoApiV._2.Entity;
using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace FunkoApiV._2.Seeders;

/// <summary>
/// Seeder explícito e idempotente para datos iniciales.
/// Respeta las relaciones: Proveedor 1 → * Funkos * → 1 Categoría.
/// </summary>
public static class DbSeeder {
    private const string ProveedorSeedId = "672f1a8b3c9d4e5f6a7b8c9d";

    public static async Task SeedAsync(AppDbContext db, AppDbContextMongoDb dbMongo, ILogger logger) {
        // ── 1. Roles de Identity (PostgreSQL) ──
        if (!await db.Roles.AnyAsync()) {
            db.Roles.AddRange(
                new Microsoft.AspNetCore.Identity.IdentityRole<long> { Name = "USER", NormalizedName = "USER" },
                new Microsoft.AspNetCore.Identity.IdentityRole<long> { Name = "ADMIN", NormalizedName = "ADMIN" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seed de roles aplicado");
        }

        // ── 2. Categorías (PostgreSQL) ──
        if (!await db.Categorias.AnyAsync()) {
            var seedDate = DateTime.UtcNow;
            db.Categorias.AddRange(
                new Categoria { Id = "VQGCKyvzIM4", Nombre = "Anime", Descripcion = "Personajes de series de anime y manga", CreatedAt = seedDate, IsDeleted = false },
                new Categoria { Id = "zw47_q9wbBE", Nombre = "Videojuegos", Descripcion = "Personajes de consolas y PC", CreatedAt = seedDate, IsDeleted = false },
                new Categoria { Id = "TcMBFSGVi1c", Nombre = "Películas", Descripcion = "Cine y series de televisión", CreatedAt = seedDate, IsDeleted = false }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seed de categorías aplicado");
        }

        // ── 3. Proveedores (MongoDB) ──
        // El ObjectId debe ser válido (24 hex) para que coincida con ProveedorId en Funkos.
        var proveedorObjectId = ObjectId.Parse(ProveedorSeedId);
        if (!await dbMongo.Proveedor.AnyAsync()) {
            var seedDate = DateTime.UtcNow;
            dbMongo.Proveedor.AddRange(
                new Proveedor {
                    Id = proveedorObjectId,
                    Nombre = "Proveedor Semilla",
                    Direccion = "Dirección semilla",
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );
            await dbMongo.SaveChangesAsync();
            logger.LogInformation("Seed de proveedores (MongoDB) aplicado");
        }

        // ── 4. Funkos (PostgreSQL) ──
        // Solo se insertan si no hay funkos, y siempre referenciando
        // categorías y proveedores que ya existen (pasos 2 y 3).
        if (!await db.Funkos.AnyAsync()) {
            var seedDate = DateTime.UtcNow;
            db.Funkos.AddRange(
                new Funko {
                    Name = "Kirito",
                    Precio = 15.99m,
                    Stock = 10,
                    Imagen = "/uploads/funkos/placeholder.png",
                    CategoriaId = "VQGCKyvzIM4",
                    ProveedorId = ProveedorSeedId,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new Funko {
                    Name = "Hinata Shoyo",
                    Precio = 14.50m,
                    Stock = 8,
                    Imagen = "/uploads/funkos/placeholder.png",
                    CategoriaId = "VQGCKyvzIM4",
                    ProveedorId = ProveedorSeedId,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new Funko {
                    Name = "Master Chief",
                    Precio = 19.99m,
                    Stock = 5,
                    Imagen = "/uploads/funkos/placeholder.png",
                    CategoriaId = "zw47_q9wbBE",
                    ProveedorId = ProveedorSeedId,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new Funko {
                    Name = "Geralt de Rivia",
                    Precio = 17.50m,
                    Stock = 12,
                    Imagen = "/uploads/funkos/placeholder.png",
                    CategoriaId = "zw47_q9wbBE",
                    ProveedorId = ProveedorSeedId,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new Funko {
                    Name = "Darth Vader",
                    Precio = 21.00m,
                    Stock = 3,
                    Imagen = "/uploads/funkos/placeholder.png",
                    CategoriaId = "TcMBFSGVi1c",
                    ProveedorId = ProveedorSeedId,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seed de funkos aplicado");
        }
    }
}