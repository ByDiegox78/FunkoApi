using FluentAssertions;
using FunkoApiV._2.Config;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Infrastructure;
using FunkoApiV._2.Models;
using FunkoApiV._2.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkoApiV2Test.Unit.Seeders;

[TestFixture]
[Category("Unit")]
public class IdentitySeederTests {
    private SqliteConnection _connection = null!;
    private ServiceProvider _services = null!;
    private IServiceScope _scope = null!;

    [SetUp]
    public async Task SetUp() {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Jwt:Key"] = "identity-seeder-test-key-that-is-at-least-32-bytes",
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests"
            })
            .Build();
        services.AddIdentityConfiguration(configuration);
        _services = services.BuildServiceProvider();
        _scope = _services.CreateScope();
        await _scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown() {
        _scope.Dispose();
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task SeedAsync_creaRolesYUsuariosYEsIdempotente() {
        // Arrange
        var provider = _scope.ServiceProvider;

        // Act
        await IdentitySeeder.SeedAsync(provider, NullLogger.Instance);
        await IdentitySeeder.SeedAsync(provider, NullLogger.Instance);

        // Assert
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<long>>>();
        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        (await roleManager.RoleExistsAsync("ADMIN")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("USER")).Should().BeTrue();
        var admin = await userManager.FindByEmailAsync("admin@admin.com");
        var user = await userManager.FindByEmailAsync("user@user.com");
        admin.Should().NotBeNull();
        user.Should().NotBeNull();
        (await userManager.IsInRoleAsync(admin!, "ADMIN")).Should().BeTrue();
        (await userManager.IsInRoleAsync(user!, "USER")).Should().BeTrue();
        (await userManager.Users.CountAsync()).Should().Be(2);
        (await roleManager.Roles.CountAsync()).Should().Be(2);
    }
}
