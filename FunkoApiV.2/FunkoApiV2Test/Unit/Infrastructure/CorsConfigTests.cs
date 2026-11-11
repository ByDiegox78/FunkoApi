using FluentAssertions;
using FunkoApiV._2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunkoApiV2Test.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class CorsConfigTests {
    [Test]
    public void AddCustomCors_combinaOrigenesConfiguradosYPorDefectoSinDuplicados() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cors:AllowedOrigins:0"] = "https://tienda.example.com",
                ["Cors:AllowedOrigins:1"] = "http://localhost:5197"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddCustomCors(configuration, isDevelopment: true);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>()
            .Value.GetPolicy(CorsConfig.PolicyName);

        // Assert
        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain("https://tienda.example.com")
            .And.Contain("http://localhost:5197")
            .And.Contain("http://localhost:5500");
        policy.SupportsCredentials.Should().BeTrue();
        policy.Headers.Should().Contain("*");
        policy.Methods.Should().Contain("*");
    }

    [Test]
    public void AddCustomCors_enProduccionSoloPermiteOrigenesConfigurados() {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cors:AllowedOrigins:0"] = "https://tienda.example.com"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddCustomCors(configuration, isDevelopment: false);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>()
            .Value.GetPolicy(CorsConfig.PolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().ContainSingle().Which.Should().Be("https://tienda.example.com");
        policy.SupportsCredentials.Should().BeTrue();
    }

    [Test]
    public void UseCustomCors_devuelveLaAplicacionConfigurada() {
        // Arrange
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        // Act
        var resultado = app.UseCustomCors();

        // Assert
        resultado.Should().BeSameAs(app);
    }
}
