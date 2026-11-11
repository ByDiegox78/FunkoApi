using FluentAssertions;
using FunkoApiV._2.GraphQL.Events;
using FunkoApiV._2.GraphQL.Resolvers;
using FunkoApiV._2.GraphQL.Subscriptions;
using FunkoApiV._2.Models;

namespace FunkoApiV2Test.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
public class FunkoGraphQLResolversTests {
    [Test]
    public void GetEstaDisponible_funkoActivo_devuelveTrue() {
        // Arrange
        var resolver = new FunkoResolvers();

        // Act
        var resultado = resolver.GetEstaDisponible(new Funko { IsDeleted = false });

        // Assert
        resultado.Should().BeTrue();
    }

    [Test]
    public void GetEstaDisponible_funkoEliminado_devuelveFalse() {
        // Arrange
        var resolver = new FunkoResolvers();

        // Act
        var resultado = resolver.GetEstaDisponible(new Funko { IsDeleted = true });

        // Assert
        resultado.Should().BeFalse();
    }

    [Test]
    public void OnFunkoCreado_devuelveElEventoRecibido() {
        // Arrange
        var evento = new FunkoCreadoEvent { FunkoId = 42, Name = "Kirito" };

        // Act
        var resultado = new FunkoSubscriptions().OnFunkoCreado(evento);

        // Assert
        resultado.Should().BeSameAs(evento);
    }

    [Test]
    public void OnFunkoActualizado_devuelveElEventoRecibido() {
        // Arrange
        var evento = new FunkoActualizadoEvent { FunkoId = 42, Name = "Kirito" };

        // Act
        var resultado = new FunkoSubscriptions().OnFunkoActualizado(evento);

        // Assert
        resultado.Should().BeSameAs(evento);
    }

    [Test]
    public void OnFunkoEliminado_devuelveElEventoRecibido() {
        // Arrange
        var evento = new FunkoEliminadoEvent { FunkoId = 42 };

        // Act
        var resultado = new FunkoSubscriptions().OnFunkoEliminado(evento);

        // Assert
        resultado.Should().BeSameAs(evento);
    }

    [Test]
    public void OnStockBajo_devuelveElEventoRecibido() {
        // Arrange
        var evento = new FunkoStockBajoEvent { FunkoId = 42, Name = "Kirito", StockActual = 1 };

        // Act
        var resultado = new FunkoSubscriptions().OnStockBajo(evento);

        // Assert
        resultado.Should().BeSameAs(evento);
    }
}
