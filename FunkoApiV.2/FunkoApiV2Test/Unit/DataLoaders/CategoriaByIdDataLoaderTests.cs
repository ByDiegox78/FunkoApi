using FluentAssertions;
using FunkoApiV._2.DataLoaders;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using GreenDonut;

namespace FunkoApiV2Test.Unit.DataLoaders;

[TestFixture]
[Category("Unit")]
public class CategoriaByIdDataLoaderTests {
    [Test]
    public async Task LoadBatchAsync_devuelveCategoriasExistentesYOmiteIdsDesconocidos() {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (var seedContext = new AppDbContext(options)) {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Categorias.Add(new Categoria {
                Id = "VQGCKyvzIM4",
                Nombre = "Anime",
                CreatedAt = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(item => item.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) => Task.FromResult(new AppDbContext(options)));
        var loader = new TestCategoriaLoader(
            factory.Object, Mock.Of<IBatchScheduler>(), new DataLoaderOptions());

        // Act
        var resultado = await loader.LoadBatch(["VQGCKyvzIM4", "NoExiste00001"], CancellationToken.None);

        // Assert
        resultado.Should().ContainKey("VQGCKyvzIM4");
        resultado["VQGCKyvzIM4"].Nombre.Should().Be("Anime");
        resultado.Should().NotContainKey("NoExiste00001");
    }

    private sealed class TestCategoriaLoader(
        IDbContextFactory<AppDbContext> factory,
        IBatchScheduler scheduler,
        DataLoaderOptions options) : CategoriaByIdDataLoader(factory, scheduler, options) {
        public Task<IReadOnlyDictionary<string, Categoria>> LoadBatch(
            IReadOnlyList<string> keys, CancellationToken cancellationToken) =>
            LoadBatchAsync(keys, cancellationToken);
    }
}
