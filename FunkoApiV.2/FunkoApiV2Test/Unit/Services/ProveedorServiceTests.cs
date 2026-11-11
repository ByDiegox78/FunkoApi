using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Proveedor;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Service;
using FunkoApiV._2.Validator.Proveedor;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MongoDB.Bson;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
[Category("Unit")]
public class ProveedorServiceTests {
    private Mock<IProveedorRepository> _repo = null!;
    private Mock<IFunkoRepository> _funkoRepo = null!;
    private ProveedorService _service = null!;

    private static Proveedor ProveedorSeed => new() {
        Id = ObjectId.GenerateNewId(), Nombre = "Acme", Direccion = "Calle 1",
        CreatedAt = DateTime.UtcNow, IsDeleted = false
    };

    [SetUp]
    public void SetUp() {
        _repo = new Mock<IProveedorRepository>();
        _funkoRepo = new Mock<IFunkoRepository>();
        _service = new ProveedorService(_repo.Object, _funkoRepo.Object,
            NullLogger<ProveedorService>.Instance, new ProveedorValidator());
    }

    [Test]
    public async Task GetById_ConIdExistente_DevuelveDto() {
        var p = ProveedorSeed;
        _repo.Setup(r => r.GetById(p.Id.ToString()))
            .ReturnsAsync(Result.Success<Proveedor, DomainError>(p));
        var r = await _service.GetById(p.Id.ToString());
        r.IsSuccess.Should().BeTrue();
        r.Value.Nombre.Should().Be("Acme");
    }

    [Test]
    public async Task GetById_ConIdInexistente_DevuelveNotFound() {
        _repo.Setup(r => r.GetById("000000000000000000000000"))
            .ReturnsAsync(Result.Failure<Proveedor, DomainError>(NotFound.FromId("000000000000000000000000", "Proveedor")));
        var r = await _service.GetById("000000000000000000000000");
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task GetAll_DevuelveLosProveedoresMapeados() {
        var proveedor = ProveedorSeed;
        _repo.Setup(r => r.GetAll()).ReturnsAsync(new List<Proveedor> { proveedor });

        var resultado = await _service.GetAll();

        resultado.Should().ContainSingle().Which.Nombre.Should().Be("Acme");
    }

    [Test]
    public async Task GetDeleted_DevuelveLosProveedoresEliminados() {
        var eliminado = ProveedorSeed with { IsDeleted = true, Nombre = "Acme eliminado" };
        _repo.Setup(r => r.GetAllDeleted()).ReturnsAsync(new List<Proveedor> { eliminado });

        var resultado = await _service.GetDeleted();

        resultado.Should().ContainSingle().Which.Nombre.Should().Be("Acme eliminado");
    }

    [Test]
    public async Task Add_ConDatosValidos_DevuelveDto() {
        var p = ProveedorSeed;
        _repo.Setup(r => r.Add(It.IsAny<Proveedor>()))
            .ReturnsAsync(Result.Success<Proveedor, DomainError>(p));
        var r = await _service.Add(new ProveedorRequestDto { Nombre = "Acme", Direccion = "Calle 1" });
        r.IsSuccess.Should().BeTrue();
        r.Value.Nombre.Should().Be("Acme");
    }

    [Test]
    public async Task Add_ConNombreVacio_DevuelveValidationError() {
        var r = await _service.Add(new ProveedorRequestDto { Nombre = "", Direccion = "Calle 1" });
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(r => r.Add(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task Add_CuandoRepositorioFalla_PropagaElError() {
        _repo.Setup(r => r.Add(It.IsAny<Proveedor>()))
            .ReturnsAsync(Result.Failure<Proveedor, DomainError>(
                new Conflict("No se pudo crear el proveedor")));

        var resultado = await _service.Add(new ProveedorRequestDto {
            Nombre = "Acme", Direccion = "Calle 1"
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
    }

    [Test]
    public async Task Update_ConDatosValidos_DevuelveProveedorActualizado() {
        var proveedor = ProveedorSeed with {
            Nombre = "Acme actualizado", Direccion = "Calle 2", UpdatedAt = DateTime.UtcNow
        };
        _repo.Setup(r => r.Update(proveedor.Id.ToString(), It.IsAny<Proveedor>()))
            .ReturnsAsync(Result.Success<Proveedor, DomainError>(proveedor));

        var resultado = await _service.Update(proveedor.Id.ToString(), new ProveedorRequestDto {
            Nombre = proveedor.Nombre, Direccion = proveedor.Direccion
        });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Acme actualizado");
        resultado.Value.Direccion.Should().Be("Calle 2");
    }

    [Test]
    public async Task Update_ConDireccionInvalida_NoLlamaAlRepositorio() {
        var resultado = await _service.Update("672f1a8b3c9d4e5f6a7b8c9d",
            new ProveedorRequestDto { Nombre = "Acme", Direccion = "X" });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task Update_CuandoRepositorioNoEncuentraProveedor_PropagaNotFound() {
        const string id = "672f1a8b3c9d4e5f6a7b8c9d";
        _repo.Setup(r => r.Update(id, It.IsAny<Proveedor>()))
            .ReturnsAsync(Result.Failure<Proveedor, DomainError>(NotFound.FromId(id, "Proveedor")));

        var resultado = await _service.Update(id, new ProveedorRequestDto {
            Nombre = "Acme actualizado", Direccion = "Calle 2"
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task SoftDelete_ConFunkosAsociados_DevuelveConflict() {
        var p = ProveedorSeed;
        _funkoRepo.Setup(f => f.GetPagedAsync(It.IsAny<FunkoFilterRequest>()))
            .ReturnsAsync((new List<Funko> { new() { Id = 1 } }, 1));
        var r = await _service.SoftDelete(p.Id.ToString());
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<Conflict>();
        _funkoRepo.Verify(f => f.GetPagedAsync(It.Is<FunkoFilterRequest>(filter =>
            filter.ProveedorId == p.Id.ToString() && filter.Page == 0 && filter.Size == 1)), Times.Once);
        _repo.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SoftDelete_SinFunkos_DevuelveTrue() {
        var p = ProveedorSeed;
        _funkoRepo.Setup(f => f.GetPagedAsync(It.IsAny<FunkoFilterRequest>()))
            .ReturnsAsync((new List<Funko>(), 0));
        _repo.Setup(r => r.SoftDelete(p.Id.ToString()))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));
        var r = await _service.SoftDelete(p.Id.ToString());
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeTrue();
    }

    [Test]
    public async Task SoftDelete_CuandoRepositorioNoEncuentraProveedor_PropagaNotFound() {
        var id = ProveedorSeed.Id.ToString();
        _funkoRepo.Setup(f => f.GetPagedAsync(It.IsAny<FunkoFilterRequest>()))
            .ReturnsAsync((new List<Funko>(), 0));
        _repo.Setup(r => r.SoftDelete(id))
            .ReturnsAsync(Result.Failure<bool, DomainError>(NotFound.FromId(id, "Proveedor")));

        var resultado = await _service.SoftDelete(id);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }
}
