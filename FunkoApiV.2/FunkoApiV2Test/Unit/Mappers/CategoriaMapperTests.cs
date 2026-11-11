using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Mapper;
using FunkoApiV._2.Models;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Mappers;

[TestFixture]
[Category("Unit")]
public class CategoriaMapperTests {
    [Test]
    public void ToDto_MapeaTodosLosCampos() {
        var c = new Categoria { Id = "VQGCKyvzIM4", Nombre = "Anime", Descripcion = "Series", CreatedAt = DateTime.UtcNow };
        var dto = c.ToDto();
        dto.Id.Should().Be("VQGCKyvzIM4");
        dto.Nombre.Should().Be("Anime");
        dto.Descripcion.Should().Be("Series");
    }
}
