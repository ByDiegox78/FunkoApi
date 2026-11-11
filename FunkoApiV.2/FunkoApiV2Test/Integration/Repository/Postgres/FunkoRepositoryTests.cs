using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using Microsoft.EntityFrameworkCore;

namespace FunkoApiV2Test.Repository.Postgres;

[TestFixture]
public class FunkoRepositoryTests : PostgresBaseTest {
    private FunkoRepository _repository = null!;

    [SetUp]
    public async Task SetUpRepository() {
        _repository = new FunkoRepository(Context);
        var fecha = DateTime.UtcNow;
        Context.Funkos.AddRange(
            NuevoFunko("VQGCKyvzIM4") with { Name = "Kirito", Precio = 15.99m, Stock = 10, CreatedAt = fecha },
            NuevoFunko("VQGCKyvzIM4") with { Name = "Hinata Shoyo", Precio = 14.50m, Stock = 8, CreatedAt = fecha },
            NuevoFunko("zw47_q9wbBE") with { Name = "Master Chief", Precio = 19.99m, Stock = 5, CreatedAt = fecha },
            NuevoFunko("zw47_q9wbBE") with { Name = "Geralt de Rivia", Precio = 17.50m, Stock = 12, CreatedAt = fecha },
            NuevoFunko("TcMBFSGVi1c") with { Name = "Darth Vader", Precio = 21.00m, Stock = 3, CreatedAt = fecha });
        await Context.SaveChangesAsync();
    }

    [Test]
    public async Task GetPagedAsync_devuelve_los_funkos_sembrados_paginados() {
        var (items, total) = await _repository.GetPagedAsync(new FunkoFilterRequest { Page = 0, Size = 2 });

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Test]
    public async Task GetPagedAsync_filtra_por_categoria() {
        var (items, total) = await _repository.GetPagedAsync(
            new FunkoFilterRequest { CategoriaId = "VQGCKyvzIM4" });

        total.Should().Be(2);
        items.Should().OnlyContain(f => f.CategoriaId == "VQGCKyvzIM4");
    }

    [Test]
    public async Task GetPagedAsync_filtra_por_precio_y_ordenacion() {
        var (items, total) = await _repository.GetPagedAsync(new FunkoFilterRequest {
            PrecioMax = 18m,
            SortBy = "precio",
            Direction = "desc"
        });

        total.Should().Be(3);
        items.Select(f => f.Precio).Should().BeInDescendingOrder();
        items.Should().OnlyContain(f => f.Precio <= 18m);
    }

    [Test]
    public async Task CreateAsync_devuelve_el_funko_con_la_categoria_cargada() {
        var resultado = await _repository.CreateAsync(NuevoFunko("VQGCKyvzIM4"));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BePositive();
        resultado.Value.Categoria.Nombre.Should().Be("Anime");
        resultado.Value.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task DeleteAsync_lo_marca_como_borrado() {
        var resultado = await _repository.DeleteAsync(1);

        resultado.IsSuccess.Should().BeTrue();
        (await _repository.GetByIdAsync(1)).IsFailure.Should().BeTrue();
        (await _repository.GetPagedAsync(new FunkoFilterRequest())).Total.Should().Be(4);
    }

    [Test]
    public async Task SetImagenAsync_actualiza_la_ruta_relativa() {
        var resultado = await _repository.SetImagenAsync(1, "/uploads/funkos/nueva.png");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Imagen.Should().Be("/uploads/funkos/nueva.png");
    }

    [Test]
    public async Task EnsureSchemaAsync_creaIndicesYEsIdempotente() {
        // Arrange / Act
        await PostgresSchemaInitializer.EnsureSchemaAsync(Context);
        await PostgresSchemaInitializer.EnsureSchemaAsync(Context);

        // Assert
        var indexCount = await Context.Database.SqlQueryRaw<int>(
                """SELECT COUNT(*)::int AS "Value" FROM pg_indexes WHERE indexname = 'IX_funko_Name_Trigram_Activos'""")
            .SingleAsync();
        indexCount.Should().Be(1);
    }

    [Test]
    public async Task GetByIdAsync_existente_devuelveElFunkoConCategoria() {
        // Act
        var resultado = await _repository.GetByIdAsync(1);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Kirito");
        resultado.Value.Categoria.Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task GetPagedAsync_aplicaFiltrosYPuedeIncluirEliminados() {
        // Arrange
        await _repository.DeleteAsync(1);

        // Act
        var (items, total) = await _repository.GetPagedAsync(new FunkoFilterRequest {
            IncludeDeleted = true,
            Nombre = "Kirito",
            CategoriaId = "VQGCKyvzIM4",
            ProveedorId = ProveedorSeedId,
            PrecioMin = 15m,
            PrecioMax = 16m,
            StockMin = 10
        });

        // Assert
        total.Should().Be(1);
        items.Should().ContainSingle().Which.Name.Should().Be("Kirito");
        items[0].IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_existente_actualizaLosDatos() {
        // Arrange
        var actualizado = NuevoFunko("zw47_q9wbBE") with {
            Name = "Kirito actualizado",
            Precio = 22m,
            Stock = 7,
            Imagen = "/uploads/funkos/actualizado.png"
        };

        // Act
        var resultado = await _repository.UpdateAsync(1, actualizado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be(actualizado.Name);
        resultado.Value.CategoriaId.Should().Be("zw47_q9wbBE");
        resultado.Value.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_inexistente_devuelveNotFound() {
        // Act
        var resultado = await _repository.UpdateAsync(999, NuevoFunko("VQGCKyvzIM4"));

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task SetImagenAsync_inexistente_devuelveNotFound() {
        // Act
        var resultado = await _repository.SetImagenAsync(999, "/uploads/funkos/nueva.png");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task DeleteAsync_inexistente_devuelveNotFound() {
        // Act
        var resultado = await _repository.DeleteAsync(999);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    private static Funko NuevoFunko(string categoriaId) => new() {
        Name = "Nuevo",
        Precio = 10m,
        Stock = 2,
        Imagen = "/uploads/funkos/nuevo.png",
        CategoriaId = categoriaId,
        ProveedorId = ProveedorSeedId
    };
}
