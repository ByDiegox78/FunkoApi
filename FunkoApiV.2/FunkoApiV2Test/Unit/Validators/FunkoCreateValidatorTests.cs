using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Validator;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Validators;

[TestFixture]
[Category("Unit")]
public class FunkoCreateValidatorTests {
    private FunkoCreateValidator _validator = null!;
    [SetUp] public void SetUp() => _validator = new FunkoCreateValidator();

    [Test] public async Task Validate_ConDatosValidos_EsValido() {
        var r = await _validator.ValidateAsync(Valido());
        r.IsValid.Should().BeTrue();
    }

    [TestCase(0.01)]
    [TestCase(50.0)]
    public async Task Validate_ConPrecioEnLosLimites_EsValido(decimal precio) {
        var r = await _validator.ValidateAsync(Valido() with { Precio = precio });
        r.IsValid.Should().BeTrue();
        r.Errors.Should().BeEmpty();
    }

    [Test] public async Task Validate_ConNombreVacio_TieneErrorEnName() {
        var r = await _validator.ValidateAsync(Valido() with { Name = "" });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
    [Test] public async Task Validate_ConPrecioFueraDeRango_TieneErrorEnPrecio() {
        var r = await _validator.ValidateAsync(Valido() with { Precio = 100m });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Precio");
    }
    [Test] public async Task Validate_ConCategoriaIdDeLongitudIncorrecta_TieneError() {
        var r = await _validator.ValidateAsync(Valido() with { CategoriaId = "Anime" });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "CategoriaId");
    }
    [Test] public async Task Validate_ConProveedorIdInvalido_TieneError() {
        var r = await _validator.ValidateAsync(Valido() with { ProveedorId = "123" });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "ProveedorId");
    }
    [Test] public async Task Validate_ConStockNegativo_TieneError() {
        var r = await _validator.ValidateAsync(Valido() with { Stock = -1 });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Stock");
    }
    [Test] public async Task Validate_ConImagenUrlHttp_EsValido() {
        var r = await _validator.ValidateAsync(Valido() with { Imagen = "https://x.com/a.png" });
        r.IsValid.Should().BeTrue();
    }

    [Test] public async Task Validate_ConImagenUrlNoHttp_TieneError() {
        var r = await _validator.ValidateAsync(Valido() with { Imagen = "file:///C:/funko.png" });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Imagen");
    }

    [Test]
    public async Task Validate_ConVariosCamposInvalidos_DevuelveTodosLosErrores() {
        var r = await _validator.ValidateAsync(Valido() with {
            Name = "",
            Precio = 0,
            Stock = -1,
            CategoriaId = "Anime",
            ProveedorId = "123",
            Imagen = "file:///C:/funko.png"
        });

        r.IsValid.Should().BeFalse();
        r.Errors.Select(e => e.PropertyName).Should().Contain(
            ["Name", "Precio", "Stock", "CategoriaId", "ProveedorId", "Imagen"]);
    }

    [Test]
    public async Task Validate_ConUnSoloCampoInvalido_SoloMarcaEseCampo() {
        var r = await _validator.ValidateAsync(Valido() with { Stock = -1 });

        r.IsValid.Should().BeFalse();
        r.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Stock");
    }

    private static FunkoCreateRequest Valido() => new() {
        Name = "Kirito", Precio = 15.99m, Stock = 10,
        CategoriaId = "VQGCKyvzIM4", ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d",
        Imagen = "/uploads/funkos/a.png"
    };
}
