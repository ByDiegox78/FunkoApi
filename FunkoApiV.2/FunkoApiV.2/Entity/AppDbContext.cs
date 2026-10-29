using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApiV._2.Entity;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<Funko> Funkos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Funko>(e => {
            e.ToTable("funko");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(20);
            e.Property(x => x.Precio)
                .IsRequired()
                .HasPrecision(20, 2);
            e.Property(x => x.Stock)
                .IsRequired();
            e.Property(x => x.CategoriaId)
                .IsRequired();
            e.HasOne(f => f.Categoria)
                .WithMany(c => c.Funkos)
                .HasForeignKey(f => f.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CreatedAt)
                .IsRequired();
            e.Property(x => x.UpdatedAt)
                .IsRequired(false);
            e.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            e.HasQueryFilter(f => !f.IsDeleted);
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            e.HasData(
                new Funko { Id = 1, Name = "Kirito", Precio = 15.99m, Stock = 10, CategoriaId = 1, CreatedAt = seedDate, IsDeleted = false },
                new Funko { Id = 2, Name = "Hinata Shoyo", Precio = 14.50m, Stock = 8, CategoriaId = 1, CreatedAt = seedDate, IsDeleted = false },
                new Funko { Id = 3, Name = "Master Chief", Precio = 19.99m, Stock = 5, CategoriaId = 2, CreatedAt = seedDate, IsDeleted = false },
                new Funko { Id = 4, Name = "Geralt de Rivia", Precio = 17.50m, Stock = 12, CategoriaId = 2, CreatedAt = seedDate, IsDeleted = false },
                new Funko { Id = 5, Name = "Darth Vader", Precio = 21.00m, Stock = 3, CategoriaId = 3, CreatedAt = seedDate, IsDeleted = false }
            );
        });
        modelBuilder.Entity<Categoria>(e => {
            e.ToTable("categoria");
            e.HasKey(x => x.Id);

            e.Property(x => x.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(x => x.Descripcion)
                .HasMaxLength(500);
            e.HasData(
                new Categoria { Id = 1, Nombre = "Anime", Descripcion = "Personajes de series de anime y manga" },
                new Categoria { Id = 2, Nombre = "Videojuegos", Descripcion = "Personajes de consolas y PC" },
                new Categoria { Id = 3, Nombre = "Películas", Descripcion = "Cine y series de televisión" }
            );
        });
    }
}