using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Realtime;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Repository.Cassandra;
using FunkoApiV._2.Service;
using FunkoApiV._2.Storage;
using FunkoApiV._2.Validator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MongoDB.Bson;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
[Category("Unit")]
public class FunkoServiceTests {
    private const string ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d";

    private Mock<IFunkoRepository> _repo = null!;
    private Mock<IProveedorRepository> _repoProveedor = null!;
    private Mock<ICategoriaRepository> _repoCategoria = null!;
    private Mock<ICassandraRepository> _repoCassandra = null!;
    private Mock<IStorageService> _storage = null!;
    private Mock<IHubContext<FunkosHub>> _hubContext = null!;
    
    private FunkoService _service = null!;

    private static Proveedor ProveedorSeed => new() {
        Id = ObjectId.Parse(ProveedorId), Nombre = "Proveedor Test", CreatedAt = DateTime.UtcNow, IsDeleted = false
    };
    private static Categoria CategoriaSeed => new() {
        Id = "VQGCKyvzIM4", Nombre = "Anime", CreatedAt = DateTime.UtcNow, IsDeleted = false
    };

    [SetUp]
    public void SetUp() {
        _repo = new Mock<IFunkoRepository>();
        _repoProveedor = new Mock<IProveedorRepository>();
        _repoCategoria = new Mock<ICategoriaRepository>();
        _repoCassandra = new Mock<ICassandraRepository>();
        _repoCassandra
            .Setup(repository => repository.EliminarFunkoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(Task.CompletedTask);
        _hubContext = new Mock<IHubContext<FunkosHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _storage = new Mock<IStorageService>();

        _repoCategoria.Setup(x => x.GetById("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<Categoria, DomainError>(CategoriaSeed));
        _repoCategoria.Setup(x => x.GetById(It.Is<string>(s => s != "VQGCKyvzIM4")))
            .ReturnsAsync(Result.Failure<Categoria, DomainError>(NotFound.FromId("x", "Categoria")));
        _repoProveedor.Setup(x => x.GetById(ProveedorId))
            .ReturnsAsync(Result.Success<Proveedor, DomainError>(ProveedorSeed));
        _repoProveedor.Setup(x => x.GetById(It.Is<string>(s => s != ProveedorId)))
            .ReturnsAsync(Result.Failure<Proveedor, DomainError>(NotFound.FromId("x", "Proveedor")));

        _service = new FunkoService(
            _repo.Object,
            _repoProveedor.Object,
            _repoCategoria.Object,
            _repoCassandra.Object, 
            NullLogger<FunkoService>.Instance,
            new FunkoCreateValidator(),
            new FunkoUpdateValidator(),
            _storage.Object,
            _hubContext.Object);
    }

    [Test]
    public async Task Add_ConImagenQueNoEsUrlHttpNiRutaLocal_DevuelveValidationError() {
        var request = CreateRequest() with { Imagen = "file:///C:/funko.png" };
        var resultado = await _service.Add(request);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Add_ConProveedorQueNoExiste_DevuelveConflict() {
        var request = CreateRequest() with { ProveedorId = "000000000000000000000000" };
        var resultado = await _service.Add(request);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Add_ConCategoriaQueNoExiste_DevuelveConflict() {
        var request = CreateRequest() with { CategoriaId = "NoExiste001" };
        var resultado = await _service.Add(request);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Add_ConDatosValidos_DevuelveDto() {
        var creado = new Funko {
            Id = 1, Name = "Kirito", Precio = 15.99m, Stock = 10,
            Imagen = "/uploads/funkos/a.png", CategoriaId = "VQGCKyvzIM4",
            ProveedorId = ProveedorId,
            Categoria = new Categoria { Id = "VQGCKyvzIM4", Nombre = "Anime" },
            CreatedAt = DateTime.UtcNow
        };
        _repo.Setup(x => x.CreateAsync(It.IsAny<Funko>()))
            .ReturnsAsync(Result.Success<Funko, DomainError>(creado));

        var resultado = await _service.Add(CreateRequest());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(1);
        resultado.Value.Categoria.Should().Be("Anime");
    }

    [Test]
    public async Task Add_ConStockCero_DevuelveDto() {
        var creado = new Funko {
            Id = 2, Name = "Kirito", Precio = 0.01m, Stock = 0,
            Imagen = "/uploads/funkos/a.png", CategoriaId = "VQGCKyvzIM4",
            ProveedorId = ProveedorId, Categoria = CategoriaSeed, CreatedAt = DateTime.UtcNow
        };
        _repo.Setup(x => x.CreateAsync(It.IsAny<Funko>()))
            .ReturnsAsync(Result.Success<Funko, DomainError>(creado));

        var resultado = await _service.Add(CreateRequest() with { Precio = 0.01m, Stock = 0 });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Stock.Should().Be(0);
        _repo.Verify(x => x.CreateAsync(It.Is<Funko>(f => f.Stock == 0)), Times.Once);
    }

    [TestCase(-1)]
    [TestCase(50.01)]
    public async Task Add_ConPrecioFueraDeRango_DevuelveValidationError(decimal precio) {
        var resultado = await _service.Add(CreateRequest() with { Precio = precio });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Add_ConStockNegativo_DevuelveValidationError() {
        var resultado = await _service.Add(CreateRequest() with { Stock = -1 });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.CreateAsync(It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task GetAll_DevuelvePaginaYMapeaLosFunkos() {
        var funko = new Funko {
            Id = 1, Name = "Kirito", Precio = 15.99m, Stock = 10,
            CategoriaId = "VQGCKyvzIM4", Categoria = CategoriaSeed,
            ProveedorId = ProveedorId, CreatedAt = DateTime.UtcNow
        };
        var filtro = new FunkoFilterRequest { Page = 1, Size = 5 };
        _repo.Setup(x => x.GetPagedAsync(filtro)).ReturnsAsync((new List<Funko> { funko }, 6));

        var resultado = await _service.GetAll(filtro);

        resultado.TotalCount.Should().Be(6);
        resultado.Page.Should().Be(1);
        resultado.PageSize.Should().Be(5);
        resultado.Items.Should().ContainSingle().Which.Name.Should().Be("Kirito");
    }

    [Test]
    public async Task GetById_Inexistente_DevuelveNotFound() {
        _repo.Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Funko, DomainError>(NotFound.FromId("99", "Funko")));
        var resultado = await _service.GetById(99);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task GetById_Existente_DevuelveDtoMapeado() {
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(new Funko {
                Id = 1,
                Name = "Kirito",
                Precio = 15.99m,
                CategoriaId = "VQGCKyvzIM4",
                Categoria = CategoriaSeed,
                ProveedorId = ProveedorId
            }));

        var resultado = await _service.GetById(1);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Kirito");
        resultado.Value.Categoria.Should().Be("Anime");
    }

    [Test]
    public async Task Update_ConDatosValidos_DevuelveDtoActualizado() {
        var actualizado = new Funko {
            Id = 1, Name = "Kirito Act", Precio = 20m, Stock = 5,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
            Categoria = CategoriaSeed, CreatedAt = DateTime.UtcNow
        };
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(actualizado with { Name = "Kirito" }));
        _repo.Setup(x => x.UpdateAsync(1, It.IsAny<Funko>()))
            .ReturnsAsync(Result.Success<Funko, DomainError>(actualizado));

        var resultado = await _service.Update(1, new FunkoUpdateRequest {
            Name = "Kirito Act", Precio = 20m, Stock = 5,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
            Imagen = "https://ejemplo.com/funko.png"
        });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Name.Should().Be("Kirito Act");
        _repo.Verify(x => x.UpdateAsync(1, It.IsAny<Funko>()), Times.Once);
    }

    [Test]
    public async Task Update_ConFunkoInexistente_DevuelveNotFound() {
        _repo.Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Funko, DomainError>(NotFound.FromId("99", "Funko")));

        var resultado = await _service.Update(99, new FunkoUpdateRequest {
            Name = "Kirito Act", Precio = 20m, Stock = 5,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
        _repo.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Update_ConDatosInvalidos_DevuelveValidationError() {
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(new Funko {
                Id = 1, Name = "Kirito", Precio = 15.99m, Stock = 10,
                CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
            }));

        var resultado = await _service.Update(1, new FunkoUpdateRequest {
            Name = "", Precio = 0m, Stock = -1,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        _repo.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Update_ConProveedorInexistente_DevuelveConflictYSinActualizar() {
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(new Funko {
                Id = 1, Name = "Kirito", CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
            }));

        var resultado = await _service.Update(1, new FunkoUpdateRequest {
            Name = "Kirito",
            Precio = 15m,
            Stock = 1,
            CategoriaId = "VQGCKyvzIM4",
            ProveedorId = "000000000000000000000000"
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
        _repo.Verify(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<Funko>()), Times.Never);
    }

    [Test]
    public async Task Delete_ConFunkoExistente_DevuelveExito() {
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(new Funko {
                Id = 1,
                CategoriaId = "VQGCKyvzIM4",
                ProveedorId = ProveedorId
            }));
        _repo.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        var resultado = await _service.Delete(1);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        _repo.Verify(x => x.DeleteAsync(1), Times.Once);
        _repoCassandra.Verify(repository =>
            repository.EliminarFunkoAsync("VQGCKyvzIM4", ProveedorId, 1), Times.Once);
    }

    [Test]
    public async Task Update_ConFunkoEliminado_DevuelveConflict() {
        var eliminado = new Funko {
            Id = 1, Name = "Kirito", Precio = 15.99m, Stock = 10,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId, IsDeleted = true
        };
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(eliminado));

        var resultado = await _service.Update(1, new FunkoUpdateRequest {
            Name = "Kirito", Precio = 15.99m, Stock = 10,
            CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
        });

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<Conflict>();
    }

    [Test]
    public async Task UpdateImagen_GuardaLaNuevaYBorraLaAnterior() {
        var anterior = new Funko {
            Id = 1, CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
            Imagen = "/uploads/funkos/vieja.png"
        };
        var actualizado = anterior with { Imagen = "/uploads/funkos/nueva.png" };

        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(anterior));
        _storage.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"))
            .ReturnsAsync(Result.Success<string, DomainError>("/uploads/funkos/nueva.png"));
        _repo.Setup(x => x.SetImagenAsync(1, "/uploads/funkos/nueva.png"))
            .ReturnsAsync(Result.Success<Funko, DomainError>(actualizado));

        var resultado = await _service.UpdateImagen(1, new Mock<IFormFile>().Object);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Imagen.Should().Be("/uploads/funkos/nueva.png");
        _storage.Verify(x => x.DeleteFileAsync("/uploads/funkos/vieja.png"), Times.Once);
    }

    [Test]
    public async Task UpdateImagen_SiSetImagenFalla_LimpiaLaImagenNueva() {
        var anterior = new Funko { Id = 1, CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId, Imagen = "/uploads/funkos/vieja.png" };
        _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(Result.Success<Funko, DomainError>(anterior));
        _storage.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"))
            .ReturnsAsync(Result.Success<string, DomainError>("/uploads/funkos/nueva.png"));
        _repo.Setup(x => x.SetImagenAsync(1, "/uploads/funkos/nueva.png"))
            .ReturnsAsync(Result.Failure<Funko, DomainError>(NotFound.FromId("1", "Funko")));

        var resultado = await _service.UpdateImagen(1, new Mock<IFormFile>().Object);

        resultado.IsFailure.Should().BeTrue();
        _storage.Verify(x => x.DeleteFileAsync("/uploads/funkos/nueva.png"), Times.Once);
    }

    [Test]
    public async Task UpdateImagen_SiNoExisteElFunko_NoGuardaImagen() {
        _repo.Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Funko, DomainError>(NotFound.FromId("99", "Funko")));

        var resultado = await _service.UpdateImagen(99, new Mock<IFormFile>().Object);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
        _storage.Verify(x => x.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateImagen_SiFallaElGuardado_NoActualizaNiBorraImagen() {
        var anterior = new Funko {
            Id = 1, CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
            Imagen = "/uploads/funkos/vieja.png"
        };
        var file = new Mock<IFormFile>().Object;
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(anterior));
        _storage.Setup(x => x.SaveFileAsync(file, "funkos"))
            .ReturnsAsync(Result.Failure<string, DomainError>(StorageError.ErrorGuardando()));

        var resultado = await _service.UpdateImagen(1, file);

        resultado.IsFailure.Should().BeTrue();
        _repo.Verify(x => x.SetImagenAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _storage.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateImagen_SinImagenAnterior_NoIntentaEliminarArchivo() {
        var funko = new Funko {
            Id = 1, CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId
        };
        var file = new Mock<IFormFile>().Object;
        _repo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Funko, DomainError>(funko));
        _storage.Setup(x => x.SaveFileAsync(file, "funkos"))
            .ReturnsAsync(Result.Success<string, DomainError>("/uploads/funkos/nueva.png"));
        _repo.Setup(x => x.SetImagenAsync(1, "/uploads/funkos/nueva.png"))
            .ReturnsAsync(Result.Success<Funko, DomainError>(funko with {
                Imagen = "/uploads/funkos/nueva.png"
            }));

        var resultado = await _service.UpdateImagen(1, file);

        resultado.IsSuccess.Should().BeTrue();
        _storage.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UploadImagen_ConArchivoValido_DelegaalStorageEnLaCarpetaFunko() {
        var file = new Mock<IFormFile>().Object;
        _storage.Setup(x => x.SaveFileAsync(file, "funkos"))
            .ReturnsAsync(Result.Success<string, DomainError>("/uploads/funkos/nueva.png"));

        var resultado = await _service.UploadImagen(file);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be("/uploads/funkos/nueva.png");
        _storage.Verify(x => x.SaveFileAsync(file, "funkos"), Times.Once);
    }

    [Test]
    public async Task UploadImagen_CuandoElFicheroNoEsValido_DevuelveBusinessRuleError() {
        _storage.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"))
            .ReturnsAsync(Result.Failure<string, DomainError>(StorageError.ExtensionNoPermitida()));
        var resultado = await _service.UploadImagen(new Mock<IFormFile>().Object);
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>();
    }

    private static FunkoCreateRequest CreateRequest() => new() {
        Name = "Kirito", Precio = 15.99m, Stock = 10,
        CategoriaId = "VQGCKyvzIM4", ProveedorId = ProveedorId,
        Imagen = "/uploads/funkos/a.png"
    };
}
