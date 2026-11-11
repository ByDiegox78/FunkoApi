using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Service;
using FunkoApiV._2.Validator;
using FunkoApiV2Test.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApiV2Test.Integration.Services;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CategoriaServiceIntegrationTests {
    private const string ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d";
    private PostgreSqlContainer _postgres = null!;
    private ServiceProvider _sp = null!;
    private AppDbContext _db = null!;
    private ICategoriaService _service = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await DockerTestPrerequisite.StartContainerOrIgnoreAsync(async () => {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _postgres.StartAsync();
        });
    }

    [SetUp]
    public async Task SetUp() {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddScoped<IFunkoRepository, FunkoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        // Proveedor se mockea porque vive en Mongo (responsabilidad del compañero)
        services.AddScoped<IProveedorRepository>(_ => {
            var m = new Mock<IProveedorRepository>();
            m.Setup(x => x.GetById(It.IsAny<string>()))
                .ReturnsAsync(Result.Success<Proveedor, DomainError>(new Proveedor {
                    Id = MongoDB.Bson.ObjectId.Parse(ProveedorId),
                    Nombre = "Proveedor Test",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }));
            return m.Object;
        });
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddLogging();
        services.AddTransient<IValidator<CategoriaRequestDto>, CategoriaValidator>();
        _sp = services.BuildServiceProvider();
        _db = _sp.GetRequiredService<AppDbContext>();
        await _db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await _db.Database.EnsureCreatedAsync();
        var seedDate = DateTime.UtcNow;
        _db.Categorias.AddRange(
            new Categoria {
                Id = "VQGCKyvzIM4",
                Nombre = "Anime",
                Descripcion = "Personajes de series de anime y manga",
                CreatedAt = seedDate
            },
            new Categoria {
                Id = "zw47_q9wbBE",
                Nombre = "Videojuegos",
                Descripcion = "Personajes de consolas y PC",
                CreatedAt = seedDate
            },
            new Categoria {
                Id = "TcMBFSGVi1c",
                Nombre = "Películas",
                Descripcion = "Cine y series de televisión",
                CreatedAt = seedDate
            });
        _db.Funkos.Add(new Funko {
            Name = "Kirito",
            Precio = 15.99m,
            Stock = 10,
            Imagen = "/uploads/funkos/placeholder.png",
            CategoriaId = "VQGCKyvzIM4",
            ProveedorId = ProveedorId,
            CreatedAt = seedDate
        });
        await _db.SaveChangesAsync();
        _service = _sp.GetRequiredService<ICategoriaService>();
    }

    [TearDown]
    public async Task TearDown() {
        if (_db is not null)
            await _db.DisposeAsync();
        if (_sp is not null)
            await _sp.DisposeAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    #region GetAll Tests

    [Test]
    public async Task GetAll_ConCategoriasSembradas_RetornaPagina() {
        // Act
        var resultado = await _service.GetAll(new CategoriaFilterRequest { Page = 0, Size = 10 });

        // Assert
        resultado.TotalCount.Should().Be(3);
        resultado.Items.Should().HaveCount(3);
        resultado.Items.Select(c => c.Nombre).Should().Contain(["Anime", "Videojuegos", "Películas"]);
    }

    [Test]
    public async Task GetAll_ConFiltroNombre_RetornaFiltrado() {
        // Act
        var resultado = await _service.GetAll(new CategoriaFilterRequest {
            Nombre = "nim",
            Page = 0,
            Size = 10
        });

        // Assert
        resultado.TotalCount.Should().Be(1);
        resultado.Items.Should().HaveCount(1);
        resultado.Items.Single().Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task GetAll_SinCategorias_RetornaListaVacia() {
        // Arrange — borrar todas las categorías
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM funko");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM categoria");

        // Act
        var resultado = await _service.GetAll(new CategoriaFilterRequest { Page = 0, Size = 10 });

        // Assert
        resultado.TotalCount.Should().Be(0);
        resultado.Items.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_ConIdExistente_RetornaCategoria() {
        // Act
        var resultado = await _service.GetById("VQGCKyvzIM4");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be("VQGCKyvzIM4");
        resultado.Value.Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task GetById_ConIdInexistente_RetornaNotFound() {
        // Act
        var resultado = await _service.GetById("NoExiste001");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    #endregion

    #region Add Tests

    [Test]
    public async Task Add_ConDatosValidos_CreaCategoria() {
        // Act
        var resultado = await _service.Add(new CategoriaRequestDto {
            Nombre = "Terror",
            Descripcion = "Personajes de miedo"
        });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().HaveLength(11);
        resultado.Value.Nombre.Should().Be("Terror");
        resultado.Value.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task Add_ConNombreVacio_DevuelveValidacionError() {
        // Act
        var resultado = await _service.Add(new CategoriaRequestDto { Nombre = "" });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task Add_ConNombreMuyLargo_DevuelveValidacionError() {
        // Act
        var resultado = await _service.Add(new CategoriaRequestDto {
            Nombre = new string('A', 101),
            Descripcion = "Desc"
        });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ConDatosValidos_ActualizaCategoria() {
        // Act
        var resultado = await _service.Update("VQGCKyvzIM4", new CategoriaRequestDto {
            Nombre = "Anime y Manga",
            Descripcion = "Series japonesas"
        });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be("VQGCKyvzIM4");
        resultado.Value.Nombre.Should().Be("Anime y Manga");
        resultado.Value.Descripcion.Should().Be("Series japonesas");
        resultado.Value.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Update_ConIdInexistente_RetornaNotFound() {
        // Act
        var resultado = await _service.Update("NoExiste001", new CategoriaRequestDto {
            Nombre = "Fantasma",
            Descripcion = "Y"
        });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_CategoriaSinFunkos_MarcaComoBorrada() {
        // Arrange — crear una categoría nueva sin funkos asociados
        var creada = await _service.Add(new CategoriaRequestDto {
            Nombre = "Categoria Sin Funkos",
            Descripcion = "Sin funkos"
        });
        creada.IsSuccess.Should().BeTrue();
        string idCreada = creada.Value.Id;

        // Act
        var resultado = await _service.Delete(idCreada);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        var listado = await _service.GetAll(new CategoriaFilterRequest());
        listado.Items.Should().NotContain(c => c.Id == idCreada);
    }

    [Test]
    public async Task Delete_CategoriaConFunkosAsociados_DevuelveConflict() {
        // Arrange — "VQGCKyvzIM4" tiene funkos asociados (siembrados en seed)

        // Act
        var resultado = await _service.Delete("VQGCKyvzIM4");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
        // La categoría no debe haberse borrado
        var listado = await _service.GetAll(new CategoriaFilterRequest());
        listado.Items.Should().Contain(c => c.Id == "VQGCKyvzIM4");
    }

    [Test]
    public async Task Delete_CategoriaInexistente_RetornaNotFound() {
        // Act
        var resultado = await _service.Delete("NoExiste001");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    #endregion
}
