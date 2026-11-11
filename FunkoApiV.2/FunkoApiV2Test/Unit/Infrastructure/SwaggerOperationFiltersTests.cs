using FluentAssertions;
using FunkoApiV._2.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FunkoApiV2Test.Unit.Infrastructure;

[TestFixture]
[Category("Unit")]
public class SwaggerOperationFiltersTests {
    private static OperationFilterContext ContextFor(System.Reflection.MethodInfo method) {
        var schemaGenerator = new Mock<ISchemaGenerator>();
        schemaGenerator
            .Setup(generator => generator.GenerateSchema(
                It.IsAny<Type>(),
                It.IsAny<SchemaRepository>(),
                It.IsAny<System.Reflection.MemberInfo>(),
                It.IsAny<System.Reflection.ParameterInfo>(),
                It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema());
        return new OperationFilterContext(
            new ApiDescription(),
            schemaGenerator.Object,
            new SchemaRepository(),
            new OpenApiDocument(),
            method);
    }

    [Test]
    public void AuthorizeOperationFilter_sinAtributosNoAgregaSeguridad() {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        new AuthorizeOperationFilter().Apply(
            operation, ContextFor(typeof(SwaggerOperationFiltersTests).GetMethod(
                nameof(OperationPublica), System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)!));

        // Assert
        operation.Security.Should().BeNull();
        operation.Responses.Should().BeEmpty();
    }

    [Test]
    public void AuthorizeOperationFilter_conAuthorizeAgregaSeguridadYErrores() {
        // Arrange
        var operation = new OpenApiOperation();
        var method = typeof(OperacionesAutorizadas).GetMethod(nameof(OperacionesAutorizadas.OperationAdmin))!;

        // Act
        new AuthorizeOperationFilter().Apply(operation, ContextFor(method));

        // Assert
        operation.Security.Should().ContainSingle();
        operation.Responses.Should().ContainKeys("401", "403");
        operation.Responses["401"].Description.Should().Be("No autenticado");
        operation.Responses["403"].Content.Should().ContainKey("application/json");
    }

    [Test]
    public void AuthorizeOperationFilter_allowAnonymousNoAgregaSeguridad() {
        // Arrange
        var operation = new OpenApiOperation();
        var method = typeof(OperacionesAutorizadas)
            .GetMethod(nameof(OperacionesAutorizadas.OperationPublica))!;

        // Act
        new AuthorizeOperationFilter().Apply(operation, ContextFor(method));

        // Assert
        operation.Security.Should().BeNull();
        operation.Responses.Should().BeEmpty();
    }

    [Test]
    public void ApiResponsesOperationFilter_agregaRespuesta429SinReemplazarLaExistente() {
        // Arrange
        var operation = new OpenApiOperation {
            Responses = new OpenApiResponses {
                ["429"] = new OpenApiResponse { Description = "Respuesta existente" }
            }
        };

        // Act
        new ApiResponsesOperationFilter().Apply(
            operation, ContextFor(typeof(SwaggerOperationFiltersTests).GetMethod(
                nameof(OperationPublica), System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)!));

        // Assert
        operation.Responses.Should().ContainSingle();
        operation.Responses["429"].Description.Should().Be("Respuesta existente");
    }

    [Test]
    public void ApiResponsesOperationFilter_creaRespuesta429ConEsquemaCuandoNoExiste() {
        // Arrange
        var operation = new OpenApiOperation();

        // Act
        new ApiResponsesOperationFilter().Apply(
            operation, ContextFor(typeof(SwaggerOperationFiltersTests).GetMethod(
                nameof(OperationPublica), System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)!));

        // Assert
        operation.Responses.Should().ContainKey("429");
        operation.Responses["429"].Description.Should()
            .Be("Se ha superado el límite de peticiones por IP.");
        operation.Responses["429"].Content!["application/json"].Schema.Should().NotBeNull();
    }

    private static void OperationPublica() { }

    [Authorize]
    private sealed class OperacionesAutorizadas {
        public void OperationAdmin() { }

        [AllowAnonymous]
        public void OperationPublica() { }
    }
}
