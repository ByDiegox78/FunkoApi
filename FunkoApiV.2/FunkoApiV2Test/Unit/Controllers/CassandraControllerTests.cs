using FluentAssertions;
using FunkoApiV._2.Controller.Cassandra;
using FunkoApiV._2.Dto.Cassandra;
using FunkoApiV._2.Service.Cassandra;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FunkoApiV2Test.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class CassandraControllerTests {
    private Mock<ICassandraService> _service = null!;
    private CassandraController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<ICassandraService>();
        _controller = new CassandraController(_service.Object);
    }

    [Test]
    public async Task GetFunkosPorCategoria_devuelveOkConResultados() {
        // Arrange
        IReadOnlyList<FunkosPorCategoriaDto> funkos = [new() {
            CategoriaId = "VQGCKyvzIM4",
            FunkoId = 42,
            Nombre = "Kirito"
        }];
        _service.Setup(service => service.GetFunkosPorCategoria("VQGCKyvzIM4"))
            .ReturnsAsync(funkos);

        // Act
        var resultado = await _controller.GetFunkosPorCategoria("VQGCKyvzIM4");

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(funkos);
    }

    [Test]
    public async Task GetFunkosPorProveedor_devuelveOkConResultados() {
        // Arrange
        IReadOnlyList<FunkosPorProveedorDto> funkos = [new() {
            ProveedorId = "proveedor-1",
            FunkoId = 42,
            Nombre = "Kirito"
        }];
        _service.Setup(service => service.GetFunkosPorProveedor("proveedor-1"))
            .ReturnsAsync(funkos);

        // Act
        var resultado = await _controller.GetFunkosPorProveedor("proveedor-1");

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(funkos);
    }

    [Test]
    public async Task GetFunkosPorProveedorCategoria_devuelveOkConResultados() {
        // Arrange
        IReadOnlyList<FunkoPorProveedorCategoriaDto> funkos = [new() {
            ProveedorId = "proveedor-1",
            CategoriaId = "VQGCKyvzIM4",
            FunkoId = 42,
            Nombre = "Kirito"
        }];
        _service.Setup(service => service.GetFunkosPorProveedorCategoria(
                "proveedor-1", "VQGCKyvzIM4"))
            .ReturnsAsync(funkos);

        // Act
        var resultado = await _controller.GetFunkosPorProveedorCategoria(
            "proveedor-1", "VQGCKyvzIM4");

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(funkos);
    }
}
