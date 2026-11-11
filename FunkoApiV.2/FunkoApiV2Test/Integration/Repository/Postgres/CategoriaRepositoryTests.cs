using FluentAssertions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Repository;

namespace FunkoApiV2Test.Repository.Postgres;

[TestFixture]
public class CategoriaRepositoryTests : PostgresBaseTest {
    private CategoriaRepository _repository = null!;

    [SetUp]
    public void SetUpRepository() {
        _repository = new CategoriaRepository(Context);
    }

    [Test]
    public async Task CreateAsync_genera_un_id_de_11_caracteres() {
        var creada = await _repository.CreateAsync(new Categoria {
            Nombre = "Terror",
            Descripcion = "Personajes de miedo"
        });

        creada.IsSuccess.Should().BeTrue();
        creada.Value.Id.Should().HaveLength(11);
        creada.Value.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task GetPagedAsync_devuelve_las_categorias_sembradas() {
        var (items, total) = await _repository.GetPagedAsync(new CategoriaFilterRequest());

        total.Should().Be(3);
        items.Should().HaveCount(3);
        items.Select(c => c.Nombre).Should().Contain(["Anime", "Videojuegos", "Películas"]);
    }

    [Test]
    public async Task GetPagedAsync_filtra_por_nombre() {
        var (items, total) = await _repository.GetPagedAsync(new CategoriaFilterRequest { Nombre = "nim" });

        total.Should().Be(1);
        items.Single().Nombre.Should().Be("Anime");
    }

    [Test]
    public async Task GetById_con_id_inexistente_devuelve_NotFound() {
        var resultado = await _repository.GetById("NoExiste001".PadRight(11, '0'));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task UpdateAsync_guarda_los_cambios() {
        var resultado = await _repository.UpdateAsync("VQGCKyvzIM4", new Categoria {
            Nombre = "Anime y Manga",
            Descripcion = "Series japonesas"
        });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Anime y Manga");
        resultado.Value.UpdatedAt.Should().NotBeNull();
    }

    [Test]
    public async Task DeleteAsync_la_oculta_del_listado() {
        var borrada = await _repository.DeleteAsync("VQGCKyvzIM4");

        borrada.IsSuccess.Should().BeTrue();
        var (items, total) = await _repository.GetPagedAsync(new CategoriaFilterRequest());
        total.Should().Be(2);
        items.Should().NotContain(c => c.Id == "VQGCKyvzIM4");
    }
}
