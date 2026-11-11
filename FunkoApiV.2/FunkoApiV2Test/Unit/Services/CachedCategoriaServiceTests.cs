using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiMongo.Cache;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Service;
using Moq;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
[Category("Unit")]
public class CachedCategoriaServiceTests {
    private Mock<ICategoriaService> _inner = null!;
    private Mock<ICacheService> _cache = null!;
    private CachedCategoriaService _sut = null!;

    [SetUp]
    public void SetUp() {
        _inner = new Mock<ICategoriaService>();
        _cache = new Mock<ICacheService>();
        _sut = new CachedCategoriaService(_inner.Object, _cache.Object);
    }

    [Test]
    public async Task GetAll_DelegaSinCache() {
        var filter = new CategoriaFilterRequest();
        var result = new PagedResult<CategoriaDto>();
        _inner.Setup(x => x.GetAll(filter)).ReturnsAsync(result);

        (await _sut.GetAll(filter)).Should().BeSameAs(result);
        _cache.Verify(x => x.GetAsync<CategoriaDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetById_CacheHit_NoConsultaAlServicioInterno() {
        var dto = new CategoriaDto("VQGCKyvzIM4", "Anime", null, DateTime.UtcNow, null);
        _cache.Setup(x => x.GetAsync<CategoriaDto>("categoria:VQGCKyvzIM4", default)).ReturnsAsync(dto);

        var result = await _sut.GetById("VQGCKyvzIM4");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _inner.Verify(x => x.GetById(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetById_CacheMiss_GuardaResultadoExitoso() {
        var dto = new CategoriaDto("VQGCKyvzIM4", "Anime", null, DateTime.UtcNow, null);
        _cache.Setup(x => x.GetAsync<CategoriaDto>("categoria:VQGCKyvzIM4", default)).ReturnsAsync((CategoriaDto?)null);
        _inner.Setup(x => x.GetById("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(dto));

        var result = await _sut.GetById("VQGCKyvzIM4");

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.SetAsync("categoria:VQGCKyvzIM4", dto, null, default), Times.Once);
    }

    [Test]
    public async Task GetById_ErrorInterno_NoGuardaEnCache() {
        _cache.Setup(x => x.GetAsync<CategoriaDto>("categoria:missing", default)).ReturnsAsync((CategoriaDto?)null);
        _inner.Setup(x => x.GetById("missing"))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(NotFound.FromId("missing", "Categoria")));

        var result = await _sut.GetById("missing");

        result.IsFailure.Should().BeTrue();
        _cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<CategoriaDto>(), null, default), Times.Never);
    }

    [Test]
    public async Task Add_DelegaAlServicioInterno() {
        var request = new CategoriaRequestDto { Nombre = "Anime" };
        var result = Result.Success<CategoriaDto, DomainError>(
            new CategoriaDto("VQGCKyvzIM4", "Anime", null, DateTime.UtcNow, null));
        _inner.Setup(x => x.Add(request)).ReturnsAsync(result);

        (await _sut.Add(request)).Should().Be(result);
    }

    [Test]
    public async Task Update_Exitoso_InvalidaCacheDeId() {
        var request = new CategoriaRequestDto { Nombre = "Anime" };
        var result = Result.Success<CategoriaDto, DomainError>(
            new CategoriaDto("VQGCKyvzIM4", "Anime", null, DateTime.UtcNow, null));
        _inner.Setup(x => x.Update("VQGCKyvzIM4", request)).ReturnsAsync(result);

        (await _sut.Update("VQGCKyvzIM4", request)).Should().Be(result);
        _cache.Verify(x => x.RemoveAsync("categoria:VQGCKyvzIM4", default), Times.Once);
    }

    [Test]
    public async Task Update_Falla_NoInvalidaCache() {
        var request = new CategoriaRequestDto { Nombre = "Anime" };
        var result = Result.Failure<CategoriaDto, DomainError>(NotFound.FromId("missing", "Categoria"));
        _inner.Setup(x => x.Update("missing", request)).ReturnsAsync(result);

        (await _sut.Update("missing", request)).Should().Be(result);
        _cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task Delete_Exitoso_InvalidaCacheDeId() {
        _inner.Setup(x => x.Delete("VQGCKyvzIM4"))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        (await _sut.Delete("VQGCKyvzIM4")).IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.RemoveAsync("categoria:VQGCKyvzIM4", default), Times.Once);
    }

    [Test]
    public async Task Delete_Falla_NoInvalidaCache() {
        _inner.Setup(x => x.Delete("missing"))
            .ReturnsAsync(Result.Failure<bool, DomainError>(NotFound.FromId("missing", "Categoria")));

        (await _sut.Delete("missing")).IsFailure.Should().BeTrue();
        _cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }
}
