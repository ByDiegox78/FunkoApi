using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Service;
using FunkoApiV._2.Validator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
public class CategoriaServiceTests {
    private Mock<ICategoriaRepository> _repo = null!;
    private Mock<IFunkoRepository> _funkoRepo = null!;
    private CategoriaService _service = null!;

    [SetUp]
    public void SetUp() {
        _repo = new Mock<ICategoriaRepository>();
        _funkoRepo = new Mock<IFunkoRepository>();
        _service = new CategoriaService(
            _repo.Object,
            _funkoRepo.Object,
            NullLogger<CategoriaService>.Instance,
            new CategoriaValidator());
    }
    [Test]
    public async Task Add_ConNombreVacio_DevuelveValidationError() {
        var resultado = await _service.Add(new CategoriaRequestDto { Nombre = "" });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task Add_ConDescripcionDemasiadoLarga_DevuelveValidationError() {
        var resultado = await _service.Add(new CategoriaRequestDto {
            Nombre = "Anime",
            Descripcion = new string('x', 501)
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task Add_ConDatosValidos_DevuelveDto() {
        var creada = new Categoria {
            Id = "VQGCKyvzIM4",
            Nombre = "Anime",
            Descripcion = "Anime y manga",
            CreatedAt = DateTime.UtcNow
        };
        _repo.Setup(x => x.CreateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(Result.Success<Categoria, DomainError>(creada));

        var resultado = await _service.Add(new CategoriaRequestDto { Nombre = "Anime", Descripcion = "Anime y manga" });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be("VQGCKyvzIM4");
        resultado.Value.Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task GetById_CuandoNoExiste_PropagaNotFound() {
        _repo.Setup(x => x.GetById("NoExiste001"))
            .ReturnsAsync(Result.Failure<Categoria, DomainError>(
                NotFound.FromId("NoExiste001", "Categoria")));

        var resultado = await _service.GetById("NoExiste001");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task Update_ConDatosValidos_DevuelveCategoriaActualizada() {
        var actualizada = new Categoria {
            Id = "VQGCKyvzIM4", Nombre = "Anime y Manga", Descripcion = "Series japonesas",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _repo.Setup(x => x.UpdateAsync("VQGCKyvzIM4", It.IsAny<Categoria>()))
            .ReturnsAsync(Result.Success<Categoria, DomainError>(actualizada));

        var resultado = await _service.Update("VQGCKyvzIM4", new CategoriaRequestDto {
            Nombre = actualizada.Nombre, Descripcion = actualizada.Descripcion
        });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Anime y Manga");
        resultado.Value.Descripcion.Should().Be("Series japonesas");
    }

    [Test]
    public async Task Update_ConNombreInvalido_NoLlamaAlRepositorio() {
        var resultado = await _service.Update("VQGCKyvzIM4", new CategoriaRequestDto { Nombre = "ab" });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoNoExiste_PropagaNotFound() {
        _repo.Setup(x => x.UpdateAsync("NoExiste001", It.IsAny<Categoria>()))
            .ReturnsAsync(Result.Failure<Categoria, DomainError>(
                NotFound.FromId("NoExiste001", "Categoria")));

        var resultado = await _service.Update("NoExiste001", new CategoriaRequestDto { Nombre = "Anime" });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task Delete_ConFunkosAsociados_DevuelveConflictYNoElimina() {
        _funkoRepo.Setup(x => x.GetPagedAsync(It.Is<FunkoFilterRequest>(
                f => f.CategoriaId == "VQGCKyvzIM4" && f.Page == 0 && f.Size == 1)))
            .ReturnsAsync((new List<Funko> { new() { Id = 1 } }, 1));

        var resultado = await _service.Delete("VQGCKyvzIM4");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
        _repo.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Delete_SinFunkosAsociados_EliminaCategoria() {
        _funkoRepo.Setup(x => x.GetPagedAsync(It.IsAny<FunkoFilterRequest>()))
            .ReturnsAsync((new List<Funko>(), 0));
        _repo.Setup(x => x.DeleteAsync("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        var resultado = await _service.Delete("VQGCKyvzIM4");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        _repo.Verify(x => x.DeleteAsync("VQGCKyvzIM4"), Times.Once);
    }

    [Test]
    public async Task GetAll_DevuelvePaginaConTotal() {
        var categorias = new List<Categoria> {
            new() { Id = "VQGCKyvzIM4", Nombre = "Anime", CreatedAt = DateTime.UtcNow }
        };
        _repo.Setup(x => x.GetPagedAsync(It.IsAny<CategoriaFilterRequest>()))
            .ReturnsAsync((categorias, 1));

        var resultado = await _service.GetAll(new CategoriaFilterRequest { Page = 0, Size = 10 });

        resultado.TotalCount.Should().Be(1);
        resultado.Items.Should().HaveCount(1);
        resultado.TotalPages.Should().Be(1);
        resultado.HasNextPage.Should().BeFalse();
    }
}
