using System.Security.Claims;
using System.Buffers;
using System.Text;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.GraphQL.Inputs;
using FunkoApiV._2.GraphQL.Mutation;
using FunkoApiV._2.Infrastructure;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Service;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FunkoApiV2Test.GraphQL;

[TestFixture]
[Category("Unit")]
public class GraphQLTest
{
    private const string CategoriaId = "VQGCKyvzIM4";
    private const string ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d";

    private Mock<IFunkoRepository> _funkoRepository = null!;
    private Mock<ICategoriaRepository> _categoriaRepository = null!;
    private Mock<IFunkoService> _funkoService = null!;
    private ServiceProvider _serviceProvider = null!;
    private IRequestExecutor _executor = null!;

    [SetUp]
    public async Task SetUp()
    {
        _funkoRepository = new Mock<IFunkoRepository>();
        _categoriaRepository = new Mock<ICategoriaRepository>();
        _funkoService = new Mock<IFunkoService>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"))
            .AddPolicy("RequireUser", policy => policy.RequireRole("USER", "ADMIN"));
        services.AddSingleton(_funkoRepository.Object);
        services.AddSingleton(_categoriaRepository.Object);
        services.AddSingleton(_funkoService.Object);
        services.AddGraphQLConfig(Mock.Of<IWebHostEnvironment>());

        _serviceProvider = services.BuildServiceProvider();
        _executor = await _serviceProvider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task Query_Funkos_DevuelvePaginaConFiltros()
    {
        _funkoRepository
            .Setup(repository => repository.GetPagedAsync(It.IsAny<FunkoFilterRequest>()))
            .ReturnsAsync((
                new[]
                {
                    NewFunko(1, "Kirito", 15.99m),
                    NewFunko(2, "Hinata", 14.50m)
                },
                2));

        var result = await _executor.ExecuteAsync("""
            query {
                funkos(page: 1, size: 10, nombre: "Kirito") {
                    items {
                        id
                        name
                        precio
                        stock
                        categoria
                    }
                    totalCount
                }
            }
            """);

        NoErrors(result);
        Json(result).Should().Contain("Kirito").And.Contain("Hinata").And.Contain("\"totalCount\":2");
        _funkoRepository.Verify(repository => repository.GetPagedAsync(
            It.Is<FunkoFilterRequest>(filter =>
                filter.Page == 0 &&
                filter.Size == 10 &&
                filter.Nombre == "Kirito")), Times.Once);
    }

    [Test]
    public async Task Query_FunkoById_ReturnsProductOrNullWhenMissing()
    {
        _funkoRepository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(NewFunko(1, "Kirito", 15.99m)));
        _funkoRepository
            .Setup(repository => repository.GetByIdAsync(999))
            .ReturnsAsync(Result.Failure<Funko, DomainError>(NotFound.FromId("999", "Funko")));

        var found = await _executor.ExecuteAsync("""
            query {
                funkoById(id: 1) {
                    id
                    name
                    precio
                }
            }
            """);
        var missing = await _executor.ExecuteAsync("""
            query {
                funkoById(id: 999) {
                    id
                    name
                }
            }
            """);

        NoErrors(found);
        Json(found).Should().Contain("Kirito");
        NoErrors(missing);
        Json(missing).Should().Contain("\"funkoById\":null");
    }

    [Test]
    public async Task Query_CategoriasPaged_ReturnsCategories()
    {
        _categoriaRepository
            .Setup(repository => repository.GetPagedAsync(It.IsAny<CategoriaFilterRequest>()))
            .ReturnsAsync(((IReadOnlyList<Categoria>)new List<Categoria>
            {
                new() { Id = CategoriaId, Nombre = "Anime", Descripcion = "Anime and manga" }
            }, 1));

        var result = await _executor.ExecuteAsync("""
            query {
                categoriasPaged {
                    items {
                        id
                        nombre
                        descripcion
                    }
                    totalCount
                }
            }
            """);

        NoErrors(result);
        Json(result).Should().Contain("Anime").And.Contain("Anime and manga");
    }

    [Test]
    public async Task Query_CategoriaById_ReturnsCategoryOrNullWhenMissing()
    {
        _categoriaRepository
            .Setup(repository => repository.GetById(CategoriaId))
            .ReturnsAsync(Result.Success<Categoria, DomainError>(
                new Categoria { Id = CategoriaId, Nombre = "Anime" }));
        _categoriaRepository
            .Setup(repository => repository.GetById("Missing0001"))
            .ReturnsAsync(Result.Failure<Categoria, DomainError>(
                NotFound.FromId("Missing0001", "Categoria")));

        var found = await _executor.ExecuteAsync("""
            query {
                categoriaById(id: "VQGCKyvzIM4") {
                    id
                    nombre
                }
            }
            """);
        var missing = await _executor.ExecuteAsync("""
            query {
                categoriaById(id: "Missing0001") {
                    id
                    nombre
                }
            }
            """);

        NoErrors(found);
        Json(found).Should().Contain("Anime");
        NoErrors(missing);
        Json(missing).Should().Contain("\"categoriaById\":null");
    }

    [Test]
    public async Task Mutation_CrearFunko_RequiresAdminAndReturnsCreatedFunko()
    {
        var created = NewResponse(7, "Kirito", 15.99m);
        _funkoService
            .Setup(service => service.Add(It.IsAny<FunkoCreateRequest>()))
            .ReturnsAsync(Result.Success<FunkoResponseDto, DomainError>(created));

        var denied = await _executor.ExecuteAsync("""
            mutation {
                crearFunko(input: {
                    name: "Kirito"
                    precio: 15.99
                    stock: 10
                    categoriaId: "VQGCKyvzIM4"
                    proveedorId: "672f1a8b3c9d4e5f6a7b8c9d"
                    imagen: "/uploads/funkos/kirito.png"
                }) {
                    id
                    name
                }
            }
            """);

        denied.ExpectOperationResult().Errors.Should().NotBeEmpty();
        _funkoService.Verify(service => service.Add(It.IsAny<FunkoCreateRequest>()), Times.Never);

        var authorized = OperationRequestBuilder.New()
            .SetDocument("""
                mutation {
                    crearFunko(input: {
                        name: "Kirito"
                        precio: 15.99
                        stock: 10
                        categoriaId: "VQGCKyvzIM4"
                        proveedorId: "672f1a8b3c9d4e5f6a7b8c9d"
                        imagen: "/uploads/funkos/kirito.png"
                    }) {
                        id
                        name
                        precio
                    }
                }
                """)
            .AddGlobalState(nameof(ClaimsPrincipal), AdminPrincipal())
            .Build();

        var result = await _executor.ExecuteAsync(authorized);

        NoErrors(result);
        Json(result).Should().Contain("Kirito").And.Contain("\"id\":7");
        _funkoService.Verify(service => service.Add(It.Is<FunkoCreateRequest>(input =>
            input.Name == "Kirito" &&
            input.CategoriaId == CategoriaId &&
            input.ProveedorId == ProveedorId)), Times.Once);
    }

    [Test]
    public async Task Mutation_CrearFunko_CuandoElServicioFalla_DevuelveErrorGraphQL()
    {
        _funkoService
            .Setup(service => service.Add(It.IsAny<FunkoCreateRequest>()))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("No se pudo crear")));

        var request = AdminMutation("""
            mutation {
                crearFunko(input: {
                    name: "Kirito"
                    precio: 15.99
                    stock: 10
                    categoriaId: "VQGCKyvzIM4"
                    proveedorId: "672f1a8b3c9d4e5f6a7b8c9d"
                    imagen: "/uploads/funkos/kirito.png"
                }) {
                    id
                }
            }
            """);

        var result = await _executor.ExecuteAsync(request);

        result.ExpectOperationResult().Errors.Should().NotBeEmpty();
    }

    [Test]
    public async Task Mutation_ActualizarFunko_CuandoElServicioTieneExito_DevuelveFunko()
    {
        var updated = NewResponse(7, "Kirito actualizado", 20m) with {
            UpdatedAt = DateTime.UtcNow
        };
        _funkoService
            .Setup(service => service.Update(7, It.IsAny<FunkoUpdateRequest>()))
            .ReturnsAsync(Result.Success<FunkoResponseDto, DomainError>(updated));

        var result = await new FunkoMutation().ActualizarFunko(
            7,
            NewUpdateInput(),
            _funkoService.Object,
            Mock.Of<ITopicEventSender>());

        result.Should().Be(updated);
        _funkoService.Verify(service => service.Update(7, It.Is<FunkoUpdateRequest>(input =>
            input.Name == "Kirito actualizado" && input.Precio == 20m && input.Stock == 5)), Times.Once);
    }

    [Test]
    public async Task Mutation_ActualizarFunko_CuandoElServicioFalla_LanzaErrorGraphQL()
    {
        _funkoService
            .Setup(service => service.Update(7, It.IsAny<FunkoUpdateRequest>()))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, DomainError>(
                new NotFound("Funko no encontrado")));

        var act = () => new FunkoMutation().ActualizarFunko(
            7,
            NewUpdateInput(),
            _funkoService.Object,
            Mock.Of<ITopicEventSender>());

        await act.Should().ThrowAsync<HotChocolate.GraphQLException>();
    }

    [Test]
    public async Task Mutation_ActualizarImagenFunko_CuandoServicioTieneExito_DevuelveFunko()
    {
        var response = NewResponse(7, "Kirito", 15.99m);
        _funkoService
            .Setup(service => service.UpdateImagen(7, It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(Result.Success<FunkoResponseDto, DomainError>(response));
        var file = new Mock<IFile>();
        file.SetupGet(upload => upload.Length).Returns(3);
        file.SetupGet(upload => upload.Name).Returns("kirito.png");
        file.SetupGet(upload => upload.ContentType).Returns("image/png");
        file.Setup(upload => upload.OpenReadStream()).Returns(() => new MemoryStream([1, 2, 3]));

        var result = await new FunkoMutation().ActualizarImagenFunko(
            7, _funkoService.Object, Mock.Of<ITopicEventSender>(), file.Object);

        result.Should().Be(response);
        _funkoService.Verify(service => service.UpdateImagen(
            7, It.Is<Microsoft.AspNetCore.Http.IFormFile>(upload =>
                upload.FileName == "kirito.png" && upload.Length == 3 && upload.ContentType == "image/png")),
            Times.Once);
    }

    [Test]
    public async Task Mutation_ActualizarImagenFunko_CuandoElServicioFalla_DevuelveNull()
    {
        _funkoService
            .Setup(service => service.UpdateImagen(7, It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(Result.Failure<FunkoResponseDto, DomainError>(
                new NotFound("Funko no encontrado")));
        var file = new Mock<IFile>();
        file.SetupGet(upload => upload.Length).Returns(3);
        file.SetupGet(upload => upload.Name).Returns("kirito.png");
        file.Setup(upload => upload.OpenReadStream()).Returns(() => new MemoryStream([1, 2, 3]));

        var result = await new FunkoMutation().ActualizarImagenFunko(
            7, _funkoService.Object, Mock.Of<ITopicEventSender>(), file.Object);

        result.Should().BeNull();
    }

    [Test]
    public async Task Mutation_ActualizarImagenFunko_SinLongitud_LanzaErrorGraphQL()
    {
        var file = new Mock<IFile>();
        file.SetupGet(upload => upload.Length).Returns((long?)null);

        var act = () => new FunkoMutation().ActualizarImagenFunko(
            7, _funkoService.Object, Mock.Of<ITopicEventSender>(), file.Object);

        await act.Should().ThrowAsync<HotChocolate.GraphQLException>();
        _funkoService.Verify(service => service.UpdateImagen(
            It.IsAny<long>(), It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()), Times.Never);
    }

    [Test]
    public async Task Mutation_EliminarFunko_ReturnsServiceResult()
    {
        _funkoService
            .Setup(service => service.Delete(1))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));
        _funkoService
            .Setup(service => service.Delete(999))
            .ReturnsAsync(Result.Failure<bool, DomainError>(
                NotFound.FromId("999", "Funko")));

        var deleted = await ExecuteAdminMutation("""
            mutation {
                eliminarFunko(id: 1)
            }
            """);
        var missing = await ExecuteAdminMutation("""
            mutation {
                eliminarFunko(id: 999)
            }
            """);

        NoErrors(deleted);
        Json(deleted).Should().Contain("\"eliminarFunko\":true");
        NoErrors(missing);
        Json(missing).Should().Contain("\"eliminarFunko\":false");
    }

    private Task<IExecutionResult> ExecuteAdminMutation(string document)
    {
        return _executor.ExecuteAsync(AdminMutation(document));
    }

    private static IOperationRequest AdminMutation(string document)
    {
        return OperationRequestBuilder.New()
            .SetDocument(document)
            .AddGlobalState(nameof(ClaimsPrincipal), AdminPrincipal())
            .Build();
    }

    private static UpdateFunkoInput NewUpdateInput() => new()
    {
        Name = "Kirito actualizado",
        Precio = 20m,
        Stock = 5,
        CategoriaId = CategoriaId,
        ProveedorId = ProveedorId,
        Imagen = "/uploads/funkos/kirito.png"
    };

    private static void NoErrors(IExecutionResult result)
    {
        result.ExpectOperationResult().Errors.Should().BeEmpty();
    }

    private static string Json(IExecutionResult result)
    {
        var data = result.ExpectOperationResult().Data;
        if (data is null)
            return "null";

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new JsonWriter(
            buffer,
            new System.Text.Json.JsonWriterOptions(),
            JsonNullIgnoreCondition.None);
        data.Value.Formatter.WriteDataTo(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static ClaimsPrincipal AdminPrincipal()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "ADMIN")],
            "test");
        return new ClaimsPrincipal(identity);
    }

    private static Funko NewFunko(long id, string name, decimal precio) => new()
    {
        Id = id,
        Name = name,
        Precio = precio,
        Stock = 10,
        CategoriaId = CategoriaId,
        Categoria = new Categoria { Id = CategoriaId, Nombre = "Anime" },
        ProveedorId = ProveedorId,
        CreatedAt = DateTime.UtcNow
    };

    private static FunkoResponseDto NewResponse(long id, string name, decimal precio) =>
        new(id, name, precio, 10, "/uploads/funkos/kirito.png", "Anime", CategoriaId,
            ProveedorId, DateTime.UtcNow, null);
}
