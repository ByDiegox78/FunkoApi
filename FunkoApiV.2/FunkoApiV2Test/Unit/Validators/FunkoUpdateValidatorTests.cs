using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Validator;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Validators;

[TestFixture]
[Category("Unit")]
public class FunkoUpdateValidatorTests {
    private FunkoUpdateValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new FunkoUpdateValidator();

    [Test]
    public async Task Validate_ConDatosValidosYUrlDeImagen_EsValido() {
        var resultado = await _validator.ValidateAsync(Valido() with {
            Imagen = "https://ejemplo.com/funko.png"
        });

        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_SinImagen_EsValido() {
        var resultado = await _validator.ValidateAsync(Valido() with { Imagen = null });

        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ConImagenRelativa_EsValido() {
        var resultado = await _validator.ValidateAsync(Valido() with {
            Imagen = "/uploads/funkos/funko.png"
        });

        resultado.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ConMultiplesCamposInvalidos_DevuelveErroresMixtos() {
        var resultado = await _validator.ValidateAsync(Valido() with {
            Name = "",
            Precio = 0,
            Stock = -1,
            CategoriaId = "Anime",
            ProveedorId = "123",
            Imagen = "ruta-relativa"
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Select(e => e.PropertyName).Should().Contain(
            ["Name", "Precio", "Stock", "CategoriaId", "ProveedorId", "Imagen"]);
    }

    private static FunkoUpdateRequest Valido() => new() {
        Name = "Kirito", Precio = 15.99m, Stock = 10,
        CategoriaId = "VQGCKyvzIM4", ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d"
    };
}
