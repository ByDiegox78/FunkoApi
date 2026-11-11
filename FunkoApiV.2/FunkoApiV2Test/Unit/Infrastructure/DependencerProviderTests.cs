using FluentAssertions;
using FunkoApiV._2.Infrastructure;
using FunkoApiV._2.Service;
using Microsoft.Extensions.DependencyInjection;

namespace FunkoApiV2Test.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class DependencerProviderTests {
    [Test]
    public void AddDependencies_registraServiciosDeDominioYDevuelveLaColeccion() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var resultado = services.AddDependencies();

        // Assert
        resultado.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IFunkoService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ICategoriaService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IProveedorService));
    }
}
