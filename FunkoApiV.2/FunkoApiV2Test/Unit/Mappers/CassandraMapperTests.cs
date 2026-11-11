using FluentAssertions;
using FunkoApiV._2.Mapper.Cassandra;
using FunkoApiV._2.Models.Cassandra;

namespace FunkoApiV2Test.Unit.Mappers;

[TestFixture]
[Category("Unit")]
public class CassandraMapperTests {
    [Test]
    public void ToDto_desdeProveedor_mapeaTodosLosCampos() {
        // Arrange
        var modelo = new FunkosPorProveedor {
            ProveedorId = "proveedor-1",
            FunkosId = 42,
            Nombre = "Kirito",
            Stock = 8,
            Precio = 19.99m,
            ProveedorNombre = "Acme"
        };

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.ProveedorId.Should().Be(modelo.ProveedorId);
        dto.FunkoId.Should().Be(modelo.FunkosId);
        dto.Nombre.Should().Be(modelo.Nombre);
        dto.Stock.Should().Be(modelo.Stock);
        dto.Precio.Should().Be(modelo.Precio);
        dto.ProveedorNombre.Should().Be(modelo.ProveedorNombre);
    }

    [Test]
    public void ToDto_desdeCategoria_mapeaTodosLosCampos() {
        // Arrange
        var modelo = new FunkosPorCategoria {
            CategoriaId = "VQGCKyvzIM4",
            FunkosId = 42,
            Nombre = "Kirito",
            Stock = 8,
            Precio = 19.99m,
            CategoriaNombre = "Anime"
        };

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.CategoriaId.Should().Be(modelo.CategoriaId);
        dto.FunkoId.Should().Be(modelo.FunkosId);
        dto.Nombre.Should().Be(modelo.Nombre);
        dto.Stock.Should().Be(modelo.Stock);
        dto.Precio.Should().Be(modelo.Precio);
        dto.CategoriaNombre.Should().Be(modelo.CategoriaNombre);
    }

    [Test]
    public void ToDto_desdeProveedorYCategoria_mapeaTodosLosCampos() {
        // Arrange
        var modelo = new FunkoPorProveedorCategoria {
            ProveedorId = "proveedor-1",
            CategoriaId = "VQGCKyvzIM4",
            FunkosId = 42,
            Nombre = "Kirito",
            ProveedorNombre = "Acme",
            CategoriaNombre = "Anime"
        };

        // Act
        var dto = modelo.ToDto();

        // Assert
        dto.ProveedorId.Should().Be(modelo.ProveedorId);
        dto.CategoriaId.Should().Be(modelo.CategoriaId);
        dto.FunkoId.Should().Be(modelo.FunkosId);
        dto.Nombre.Should().Be(modelo.Nombre);
        dto.ProveedorNombre.Should().Be(modelo.ProveedorNombre);
        dto.CategoriaNombre.Should().Be(modelo.CategoriaNombre);
    }
}
