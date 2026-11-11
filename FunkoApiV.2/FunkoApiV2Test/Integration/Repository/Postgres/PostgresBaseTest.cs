using FunkoApiV._2.Entity;
using FunkoApiV._2.Models;
using FunkoApiV2Test.Integration;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FunkoApiV2Test.Repository.Postgres;

public abstract class PostgresBaseTest {
    private const string ProveedorId = "672f1a8b3c9d4e5f6a7b8c9d";

    private PostgreSqlContainer? _postgres;

    protected AppDbContext Context { get; private set; } = null!;

    protected static string ProveedorSeedId => ProveedorId;

    [OneTimeSetUp]
    public async Task ClassSetUp() {
        await DockerTestPrerequisite.StartContainerOrIgnoreAsync(async () => {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _postgres.StartAsync();
        });

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres!.GetConnectionString())
            .Options;

        Context = new AppDbContext(options);
    }

    [SetUp]
    public async Task SetUp() {
        Context.ChangeTracker.Clear();
        await Context.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await Context.Database.EnsureCreatedAsync();
        Context.Categorias.AddRange(
            new Categoria {
                Id = "VQGCKyvzIM4",
                Nombre = "Anime",
                Descripcion = "Personajes de series de anime y manga",
                CreatedAt = DateTime.UtcNow
            },
            new Categoria {
                Id = "zw47_q9wbBE",
                Nombre = "Videojuegos",
                Descripcion = "Personajes de consolas y PC",
                CreatedAt = DateTime.UtcNow
            },
            new Categoria {
                Id = "TcMBFSGVi1c",
                Nombre = "Películas",
                Descripcion = "Cine y series de televisión",
                CreatedAt = DateTime.UtcNow
            });
        await Context.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task ClassTearDown() {
        if (Context is not null)
            await Context.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}
