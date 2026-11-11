using FluentAssertions;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Infrastructure.Cassandra;
using FunkoApiV._2.Models;
using FunkoApiV._2.Seeders;
using FunkoApiV2Test.Integration;
using Cassandra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using Testcontainers.Cassandra;
using Testcontainers.MongoDb;

namespace FunkoApiV2Test.Integration.Seeders;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class DbSeederTests {
    private SqliteConnection _connection = null!;
    private AppDbContext _postgres = null!;
    private MongoDbContainer? _mongoContainer;
    private AppDbContextMongoDb _mongo = null!;
    private CassandraContainer? _cassandraContainer;
#pragma warning disable NUnit1032
    private Cluster? _cluster;
#pragma warning restore NUnit1032
    private Cassandra.ISession? _cassandraSession;
    private string _keyspace = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await DockerTestPrerequisite.StartContainerOrIgnoreAsync(async () => {
            _mongoContainer = new MongoDbBuilder("mongo:7.0").Build();
            await _mongoContainer.StartAsync();
        });
        await DockerTestPrerequisite.StartContainerOrIgnoreAsync(async () => {
            _cassandraContainer = new CassandraBuilder("cassandra:5.0").Build();
            await _cassandraContainer.StartAsync();
        });
        var options = new DbContextOptionsBuilder<AppDbContextMongoDb>()
            .UseMongoDB(_mongoContainer!.GetConnectionString(), $"db-seeder-{Guid.NewGuid():N}")
            .Options;
        _mongo = new AppDbContextMongoDb(options);
        _keyspace = $"db_seeder_{Guid.NewGuid():N}";
        _cluster = Cluster.Builder()
            .AddContactPoints(_cassandraContainer!.Hostname)
            .WithPort(_cassandraContainer.GetMappedPublicPort(9042))
            .Build();
        _cassandraSession = _cluster.Connect();
        await _cassandraSession.InitializeSchemaAsync(_keyspace);
    }

    [SetUp]
    public async Task SetUp() {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _postgres = new AppDbContext(options);
        await _postgres.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown() {
        await _postgres.DisposeAsync();
        await _connection.DisposeAsync();
        var providers = await _mongo.Proveedor.ToListAsync();
        _mongo.Proveedor.RemoveRange(providers);
        await _mongo.SaveChangesAsync();
        _mongo.ChangeTracker.Clear();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await _mongo.DisposeAsync();
        if (_mongoContainer is not null)
            await _mongoContainer.DisposeAsync();
        _cassandraSession?.Dispose();
        if (_cluster is not null)
            await _cluster.ShutdownAsync();
        if (_cassandraContainer is not null)
            await _cassandraContainer.DisposeAsync();
    }

    [Test]
    public async Task SeedAsync_creaDatosInicialesYNoDuplicaAlEjecutarseDeNuevo() {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        await DbSeeder.SeedAsync(_postgres, _mongo, logger);
        await DbSeeder.SeedAsync(_postgres, _mongo, logger);

        // Assert
        (await _postgres.Roles.CountAsync()).Should().Be(2);
        (await _postgres.Categorias.CountAsync()).Should().Be(3);
        (await _mongo.Proveedor.CountAsync()).Should().Be(1);
        (await _postgres.Funkos.CountAsync()).Should().Be(5);
        (await _postgres.Funkos.Select(funko => funko.CategoriaId).Distinct().CountAsync())
            .Should().Be(3);
        var proveedor = await _mongo.Proveedor.SingleAsync();
        proveedor.Id.Should().Be(ObjectId.Parse("672f1a8b3c9d4e5f6a7b8c9d"));
        proveedor.Nombre.Should().Be("Proveedor Semilla");
    }

    [Test]
    public async Task CassandraSeedAsync_reconstruyeLasTresVistasDesdeLasFuentesDeVerdad() {
        // Arrange
        await DbSeeder.SeedAsync(_postgres, _mongo, NullLogger.Instance);
        var repository = new FunkoApiV._2.Repository.Cassandra.CassandraRepository(_cassandraSession!);

        // Act
        await CassandraSeeder.SeedAsync(_cassandraSession!, _postgres, _mongo, NullLogger.Instance);

        // Assert
        (await repository.GetFunkosPorCategoria("VQGCKyvzIM4")).Should().HaveCount(2);
        (await repository.GetFunkosPorProveedor("672f1a8b3c9d4e5f6a7b8c9d")).Should().HaveCount(5);
        (await repository.GetFunkosPorProveedorCategoria(
            "672f1a8b3c9d4e5f6a7b8c9d", "zw47_q9wbBE")).Should().HaveCount(2);
    }
}
