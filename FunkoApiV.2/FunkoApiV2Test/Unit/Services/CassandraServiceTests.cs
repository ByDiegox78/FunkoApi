using FluentAssertions;
using FunkoApiV._2.Models.Cassandra;
using FunkoApiV._2.Repository.Cassandra;
using FunkoApiV._2.Service.Cassandra;
using Moq;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
[Category("Unit")]
public class CassandraServiceTests {
    private Mock<ICassandraRepository> _repository = null!;
    private CassandraService _service = null!;

    [SetUp]
    public void SetUp() {
        _repository = new Mock<ICassandraRepository>();
        _service = new CassandraService(_repository.Object);
    }

    [Test]
    public async Task GetFunkosPorCategoria_mapeaYDevuelveResultados() {
        // Arrange
        _repository.Setup(repository => repository.GetFunkosPorCategoria("VQGCKyvzIM4"))
            .ReturnsAsync((IReadOnlyList<FunkosPorCategoria>)[new() {
                CategoriaId = "VQGCKyvzIM4",
                FunkosId = 42,
                Nombre = "Kirito",
                Stock = 8,
                Precio = 19.99m,
                CategoriaNombre = "Anime"
            }]);

        // Act
        var resultado = await _service.GetFunkosPorCategoria("VQGCKyvzIM4");

        // Assert
        resultado.Should().ContainSingle().Which.Should().BeEquivalentTo(new {
            CategoriaId = "VQGCKyvzIM4",
            FunkoId = 42L,
            Nombre = "Kirito",
            Stock = 8,
            Precio = 19.99m,
            CategoriaNombre = "Anime"
        });
        _repository.Verify(repository => repository.GetFunkosPorCategoria("VQGCKyvzIM4"), Times.Once);
    }

    [Test]
    public async Task GetFunkosPorProveedor_mapeaYDevuelveResultados() {
        // Arrange
        _repository.Setup(repository => repository.GetFunkosPorProveedor("proveedor-1"))
            .ReturnsAsync((IReadOnlyList<FunkosPorProveedor>)[new() {
                ProveedorId = "proveedor-1",
                FunkosId = 42,
                Nombre = "Kirito",
                Stock = 8,
                Precio = 19.99m,
                ProveedorNombre = "Acme"
            }]);

        // Act
        var resultado = await _service.GetFunkosPorProveedor("proveedor-1");

        // Assert
        resultado.Should().ContainSingle().Which.Should().BeEquivalentTo(new {
            ProveedorId = "proveedor-1",
            FunkoId = 42L,
            Nombre = "Kirito",
            Stock = 8,
            Precio = 19.99m,
            ProveedorNombre = "Acme"
        });
        _repository.Verify(repository => repository.GetFunkosPorProveedor("proveedor-1"), Times.Once);
    }

    [Test]
    public async Task GetFunkosPorProveedorCategoria_mapeaYDevuelveResultados() {
        // Arrange
        _repository.Setup(repository => repository.GetFunkosPorProveedorCategoria(
                "proveedor-1", "VQGCKyvzIM4"))
            .ReturnsAsync((IReadOnlyList<FunkoPorProveedorCategoria>)[new() {
                ProveedorId = "proveedor-1",
                CategoriaId = "VQGCKyvzIM4",
                FunkosId = 42,
                Nombre = "Kirito",
                ProveedorNombre = "Acme",
                CategoriaNombre = "Anime"
            }]);

        // Act
        var resultado = await _service.GetFunkosPorProveedorCategoria("proveedor-1", "VQGCKyvzIM4");

        // Assert
        resultado.Should().ContainSingle().Which.Should().BeEquivalentTo(new {
            ProveedorId = "proveedor-1",
            CategoriaId = "VQGCKyvzIM4",
            FunkoId = 42L,
            Nombre = "Kirito",
            ProveedorNombre = "Acme",
            CategoriaNombre = "Anime"
        });
        _repository.Verify(repository => repository.GetFunkosPorProveedorCategoria(
            "proveedor-1", "VQGCKyvzIM4"), Times.Once);
    }
}
