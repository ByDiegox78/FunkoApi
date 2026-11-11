using Cassandra;
using FluentAssertions;
using FunkoApiV._2.Infrastructure.Cassandra;
using FunkoApiV._2.Models.Cassandra;
using FunkoApiV._2.Repository.Cassandra;
using FunkoApiV2Test.Integration;
using Testcontainers.Cassandra;
using ISession = Cassandra.ISession;

namespace FunkoApiV2Test.Repository.Cassandra;

[TestFixture]
[Category("Integration")]
public class CassandraRepositoryTests {
    private CassandraContainer? _cassandra;
    // El cluster del driver se cierra de forma asíncrona en ClassTearDown.
#pragma warning disable NUnit1032
    private Cluster? _cluster;
#pragma warning restore NUnit1032
    private ISession _session = null!;
    private CassandraRepository _repository = null!;
    private string _categoriaId = null!;
    private string _proveedorId = null!;
    private long _funkoId;

    [OneTimeSetUp]
    public async Task ClassSetUp() {
        await DockerTestPrerequisite.StartContainerOrIgnoreAsync(async () => {
            _cassandra = new CassandraBuilder("cassandra:5.0").Build();
            await _cassandra.StartAsync();
        });

        _cluster = Cluster.Builder()
            .AddContactPoints(_cassandra!.Hostname)
            .WithPort(_cassandra.GetMappedPublicPort(9042))
            .Build();
        _session = _cluster.Connect();
        await _session.InitializeSchemaAsync("funkos_test");
        _repository = new CassandraRepository(_session);
    }

    [SetUp]
    public void SetUp() {
        var testId = Guid.NewGuid().ToString("N");
        _categoriaId = $"Categoria{testId}";
        _proveedorId = $"Proveedor{testId}";
        _funkoId = DateTime.UtcNow.Ticks;
    }

    [Test]
    public async Task GetFunkos_ParticionesVacias_DevuelveListasVacias() {
        // Arrange

        // Act
        var porCategoria = await _repository.GetFunkosPorCategoria(_categoriaId);
        var porProveedor = await _repository.GetFunkosPorProveedor(_proveedorId);
        var porProveedorYCategoria =
            await _repository.GetFunkosPorProveedorCategoria(_proveedorId, _categoriaId);

        // Assert
        porCategoria.Should().BeEmpty();
        porProveedor.Should().BeEmpty();
        porProveedorYCategoria.Should().BeEmpty();
    }

    [Test]
    public async Task SincronizarFunkoAsync_EscribeEnLasTresVistas() {
        // Arrange
        const string nombre = "Funko de prueba";
        const string categoriaNombre = "Categoria de prueba";
        const string proveedorNombre = "Proveedor de prueba";

        // Act
        await _repository.SincronizarFunkoAsync(
            _funkoId, nombre, 8, 19.99m, _categoriaId, categoriaNombre, _proveedorId, proveedorNombre);
        var porCategoria = await _repository.GetFunkosPorCategoria(_categoriaId);
        var porProveedor = await _repository.GetFunkosPorProveedor(_proveedorId);
        var porProveedorYCategoria =
            await _repository.GetFunkosPorProveedorCategoria(_proveedorId, _categoriaId);

        // Assert
        porCategoria.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FunkosPorCategoria {
                CategoriaId = _categoriaId,
                FunkosId = _funkoId,
                Nombre = nombre,
                Stock = 8,
                Precio = 19.99m,
                CategoriaNombre = categoriaNombre
            });
        porProveedor.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FunkosPorProveedor {
                ProveedorId = _proveedorId,
                FunkosId = _funkoId,
                Nombre = nombre,
                Stock = 8,
                Precio = 19.99m,
                ProveedorNombre = proveedorNombre
            });
        porProveedorYCategoria.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FunkoPorProveedorCategoria {
                ProveedorId = _proveedorId,
                CategoriaId = _categoriaId,
                FunkosId = _funkoId,
                Nombre = nombre,
                ProveedorNombre = proveedorNombre,
                CategoriaNombre = categoriaNombre
            });
    }

    [Test]
    public async Task GetFunkosPorCategoria_SoloDevuelveElementosDeLaCategoriaSolicitada() {
        // Arrange
        await _repository.SincronizarFunkoAsync(
            _funkoId, "Funko de prueba", 8, 19.99m, _categoriaId, "Categoria", _proveedorId, "Proveedor");
        await _repository.SincronizarFunkoAsync(
            _funkoId + 1, "Otro funko", 2, 9.99m, "OtraCategoria", "Otra", "OtroProveedor", "Otro");

        // Act
        var funkos = await _repository.GetFunkosPorCategoria(_categoriaId);

        // Assert
        funkos.Should().ContainSingle();
        funkos[0].FunkosId.Should().Be(_funkoId);
        funkos[0].CategoriaId.Should().Be(_categoriaId);
    }

    [Test]
    public async Task GetFunkosPorProveedor_SoloDevuelveElementosDelProveedorSolicitado() {
        // Arrange
        await _repository.SincronizarFunkoAsync(
            _funkoId, "Funko de prueba", 8, 19.99m, _categoriaId, "Categoria", _proveedorId, "Proveedor");
        await _repository.SincronizarFunkoAsync(
            _funkoId + 1, "Otro funko", 2, 9.99m, "OtraCategoria", "Otra", "OtroProveedor", "Otro");

        // Act
        var funkos = await _repository.GetFunkosPorProveedor(_proveedorId);

        // Assert
        funkos.Should().ContainSingle();
        funkos[0].FunkosId.Should().Be(_funkoId);
        funkos[0].ProveedorId.Should().Be(_proveedorId);
    }

    [Test]
    public async Task GetFunkosPorProveedorCategoria_SoloDevuelveLaCombinacionSolicitada() {
        // Arrange
        await _repository.SincronizarFunkoAsync(
            _funkoId, "Funko coincidente", 8, 19.99m, _categoriaId, "Categoria", _proveedorId, "Proveedor");
        await _repository.SincronizarFunkoAsync(
            _funkoId + 1, "Otra categoria", 2, 9.99m, "OtraCategoria", "Otra", _proveedorId, "Proveedor");
        await _repository.SincronizarFunkoAsync(
            _funkoId + 2, "Otro proveedor", 3, 12.99m, _categoriaId, "Categoria", "OtroProveedor", "Otro");

        // Act
        var funkos = await _repository.GetFunkosPorProveedorCategoria(_proveedorId, _categoriaId);

        // Assert
        funkos.Should().ContainSingle();
        funkos[0].FunkosId.Should().Be(_funkoId);
        funkos[0].ProveedorId.Should().Be(_proveedorId);
        funkos[0].CategoriaId.Should().Be(_categoriaId);
    }

    [Test]
    public async Task EliminarFunkoAsync_BorraElFunkoDeLasTresVistas() {
        // Arrange
        await _repository.SincronizarFunkoAsync(
            _funkoId, "Funko de prueba", 8, 19.99m, _categoriaId, "Categoria", _proveedorId, "Proveedor");

        // Act
        await _repository.EliminarFunkoAsync(_categoriaId, _proveedorId, _funkoId);
        var porCategoria = await _repository.GetFunkosPorCategoria(_categoriaId);
        var porProveedor = await _repository.GetFunkosPorProveedor(_proveedorId);
        var porProveedorYCategoria =
            await _repository.GetFunkosPorProveedorCategoria(_proveedorId, _categoriaId);

        // Assert
        porCategoria.Should().BeEmpty();
        porProveedor.Should().BeEmpty();
        porProveedorYCategoria.Should().BeEmpty();
    }

    [OneTimeTearDown]
    public async Task ClassTearDown() {
        _session?.Dispose();
        if (_cluster is not null)
            await _cluster.ShutdownAsync();
        if (_cassandra is not null)
            await _cassandra.DisposeAsync();
    }
}
