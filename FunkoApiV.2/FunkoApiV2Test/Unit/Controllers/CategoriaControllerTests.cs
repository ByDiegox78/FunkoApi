using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Controller;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Controllers;

[TestFixture]
[Category("Unit")]
public class CategoriaControllerTests {
    private Mock<ICategoriaService> _service = null!;
    private CategoriaController _controller = null!;

    [SetUp]
    public void SetUp() {
        _service = new Mock<ICategoriaService>();
        _controller = new CategoriaController(_service.Object);
    }

    #region GetAll Tests

    [Test]
    public async Task GetAll_DevuelveOkConPagina() {
        // Arrange
        _service.Setup(s => s.GetAll(It.IsAny<CategoriaFilterRequest>()))
            .ReturnsAsync(new PagedResult<CategoriaDto> {
                Items = [], TotalCount = 0, Page = 0, PageSize = 10
            });

        // Act
        var r = await _controller.GetAll(new CategoriaFilterRequest());

        // Assert
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_CuandoExiste_Devuelve200() {
        // Arrange
        var dto = new CategoriaDto("VQGCKyvzIM4", "Anime", null, DateTime.UtcNow, null);
        _service.Setup(s => s.GetById("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(dto));

        // Act
        var r = await _controller.GetById("VQGCKyvzIM4");

        // Assert
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetById_CuandoNoExiste_Devuelve404() {
        // Arrange
        _service.Setup(s => s.GetById("NoExiste001"))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(NotFound.FromId("NoExiste001", "Categoria")));

        // Act
        var r = await _controller.GetById("NoExiste001");

        // Assert
        r.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Test]
    public async Task Create_ConDatosValidos_Devuelve201() {
        // Arrange
        var dto = new CategoriaDto("Nueva000001", "Nueva", null, DateTime.UtcNow, null);
        _service.Setup(s => s.Add(It.IsAny<CategoriaRequestDto>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(dto));

        // Act
        var r = await _controller.Create(new CategoriaRequestDto { Nombre = "Nueva" });

        // Assert
        r.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task Create_ConValidacionFallida_Devuelve400() {
        // Arrange
        _service.Setup(s => s.Add(It.IsAny<CategoriaRequestDto>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(new ValidationError("El nombre es obligatorio")));

        // Act
        var r = await _controller.Create(new CategoriaRequestDto { Nombre = "" });

        // Assert
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ConDatosValidos_Devuelve200() {
        // Arrange
        var dto = new CategoriaDto("VQGCKyvzIM4", "Anime Actualizada", null, DateTime.UtcNow, null);
        _service.Setup(s => s.Update("VQGCKyvzIM4", It.IsAny<CategoriaRequestDto>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(dto));

        // Act
        var r = await _controller.Update("VQGCKyvzIM4", new CategoriaRequestDto { Nombre = "Anime Actualizada" });

        // Assert
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task Update_ConIdNoExistente_Devuelve404() {
        // Arrange
        _service.Setup(s => s.Update("NoExiste001", It.IsAny<CategoriaRequestDto>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(NotFound.FromId("NoExiste001", "Categoria")));

        // Act
        var r = await _controller.Update("NoExiste001", new CategoriaRequestDto { Nombre = "X" });

        // Assert
        r.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_CuandoExiste_Devuelve204() {
        // Arrange
        _service.Setup(s => s.Delete("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        // Act
        var r = await _controller.Delete("VQGCKyvzIM4");

        // Assert
        r.Result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_CuandoNoExiste_Devuelve404() {
        // Arrange
        _service.Setup(s => s.Delete("NoExiste001"))
            .ReturnsAsync(Result.Failure<bool, DomainError>(NotFound.FromId("NoExiste001", "Categoria")));

        // Act
        var r = await _controller.Delete("NoExiste001");

        // Assert
        r.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
