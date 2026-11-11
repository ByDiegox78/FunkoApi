using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Repository.Cassandra;
using FunkoApiV._2.Service;
using FunkoApiV._2.Storage;
using FunkoApiV._2.Validator;
using FunkoApiV2Test.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Bson;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApiV2Test.Integration.Services;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class FunkoServiceIntegrationTests {
    private const string ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d";
    private PostgreSqlContainer _postgres = null!;
    private ServiceProvider _sp = null!;
    private AppDbContext _db = null!;
    private IFunkoService _service = null!;

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
        var cassandraRepository = new Mock<ICassandraRepository>();
        cassandraRepository
            .Setup(repository => repository.SincronizarFunkoAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        cassandraRepository
            .Setup(repository => repository.EliminarFunkoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton(cassandraRepository.Object);
        services.AddScoped<IProveedorRepository>(_ => {
            var m = new Mock<IProveedorRepository>();
            m.Setup(x => x.GetById(It.IsAny<string>()))
                .ReturnsAsync(Result.Success<Proveedor, DomainError>(new Proveedor { Id = ObjectId.Parse(ProveedorId), Nombre = "P", CreatedAt = DateTime.UtcNow, IsDeleted = false }));
            return m.Object;
        });
        services.AddScoped<IFunkoService, FunkoService>();
        services.AddRealtimeSignalR(isDevelopment: false);
        services.AddSingleton<IStorageService>(new Mock<IStorageService>().Object);
        services.AddLogging();
        services.AddTransient<IValidator<FunkoCreateRequest>, FunkoCreateValidator>();
        services.AddTransient<IValidator<FunkoUpdateRequest>, FunkoUpdateValidator>();
        _sp = services.BuildServiceProvider();
        _db = _sp.GetRequiredService<AppDbContext>();
        await _db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await _db.Database.EnsureCreatedAsync();
        _db.Categorias.Add(new FunkoApiV._2.Models.Categoria {
            Id = "VQGCKyvzIM4",
            Nombre = "Anime",
            Descripcion = "Personajes de series de anime y manga",
            CreatedAt = DateTime.UtcNow
        });
        _db.Funkos.Add(new FunkoApiV._2.Models.Funko {
            Name = "Kirito",
            Precio = 15.99m,
            Stock = 10,
            Imagen = "/uploads/funkos/placeholder.png",
            CategoriaId = "VQGCKyvzIM4",
            ProveedorId = ProveedorId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        _service = _sp.GetRequiredService<IFunkoService>();
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

    [Test]
    public async Task CreateAsync_ConDatosValidos_CreaYDevuelveDto() {
        var req = new FunkoCreateRequest {
            Name = "Kirito", Precio = 15.99m, Stock = 10,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
            Imagen = "/uploads/funkos/placeholder.png"
        };
        var r = await _service.Add(req);
        r.IsSuccess.Should().BeTrue();
        r.Value.Id.Should().BeGreaterThan(0);
        r.Value.CategoriaId.Should().Be("VQGCKyvzIM4");
    }

    [Test]
    public async Task GetById_Inexistente_DevuelveNotFound() {
        var r = await _service.GetById(999);
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task Delete_MarcaComoBorrado() {
        var id = 1; // funko sembrado
        var r = await _service.Delete(id);
        r.IsSuccess.Should().BeTrue();
        (await _db.Funkos.IgnoreQueryFilters().FirstAsync(f => f.Id == id)).IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConCategoriaInexistente_DevuelveNotFound() {
        var req = new FunkoCreateRequest {
            Name = "Kirito", Precio = 15.99m, Stock = 10,
            CategoriaId = "NoExiste001", ProveedorId = ProveedorId,
            Imagen = "/uploads/funkos/placeholder.png"
        };
        var r = await _service.Add(req);
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<Conflict>();
    }
}
