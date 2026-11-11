using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApiV._2.Config;
using FunkoApiV._2.Dto.Auth;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;
using FunkoApiV._2.Service;
using FunkoApiV._2.Service.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace FunkoApiV2Test.Unit.Services;

[TestFixture]
[Category("Unit")]
public class AuthServiceTests {
    private const string JwtKey = "unit-test-key-that-is-at-least-32-bytes-long";
    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;
    private Mock<UserManager<AppUser>> _userManager = null!;
    private Mock<SignInManager<AppUser>> _signInManager = null!;
    private Mock<IEmailMessage> _emailService = null!;
    private AuthService _sut = null!;

    [SetUp]
    public async Task SetUp() {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var store = new Mock<IUserStore<AppUser>>();
        _userManager = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInManager = new Mock<SignInManager<AppUser>>(
            _userManager.Object,
            new HttpContextAccessor(),
            claimsFactory.Object,
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<AppUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<AppUser>>());
        _emailService = new Mock<IEmailMessage>();
        _emailService.Setup(service => service.SendEmailAsync(It.IsAny<EmailMessage>()))
            .Returns(Task.CompletedTask);

        _sut = new AuthService(
            _context,
            _userManager.Object,
            _signInManager.Object,
            _emailService.Object,
            Options.Create(new JwtSettings {
                Key = JwtKey, Issuer = "tests", Audience = "tests", ExpirationMin = 30
            }));
    }

    [TearDown]
    public async Task TearDown() {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task Login_UsuarioInexistente_DevuelveUnauthorized() {
        _userManager.Setup(x => x.FindByEmailAsync("missing@example.com")).ReturnsAsync((AppUser?)null);

        var result = await _sut.Login(new LoginDto("missing@example.com", "Password1!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
        _signInManager.Verify(x => x.CheckPasswordSignInAsync(
            It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task Login_ContrasenaIncorrecta_DevuelveUnauthorized() {
        var user = NewUser();
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _signInManager.Setup(x => x.CheckPasswordSignInAsync(user, "wrong", false))
            .ReturnsAsync(SignInResult.Failed);

        var result = await _sut.Login(new LoginDto(user.Email!, "wrong"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task Login_ConCredencialesValidas_GeneraTokenYUsaRol() {
        var user = NewUser();
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _signInManager.Setup(x => x.CheckPasswordSignInAsync(user, "Password1!", false))
            .ReturnsAsync(SignInResult.Success);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(["ADMIN"]);

        var result = await _sut.Login(new LoginDto(user.Email!, "Password1!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("ADMIN");
        result.Value.Email.Should().Be(user.Email);
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.Value.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Login_SinRoles_UsaRolUser() {
        var user = NewUser();
        _userManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _signInManager.Setup(x => x.CheckPasswordSignInAsync(user, "Password1!", false))
            .ReturnsAsync(SignInResult.Success);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _sut.Login(new LoginDto(user.Email!, "Password1!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("USER");
    }

    [Test]
    public async Task Signup_EmailExistente_DevuelveConflict() {
        var existing = NewUser();
        _userManager.Setup(x => x.FindByEmailAsync(existing.Email!)).ReturnsAsync(existing);

        var result = await _sut.Signup(new SignupDto(existing.Email!, "Password1!", "User", "Name"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<Conflict>();
        _userManager.Verify(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Signup_CuandoIdentityRechazaElUsuario_DevuelveBusinessRuleError() {
        _userManager.Setup(x => x.FindByEmailAsync("new@example.com")).ReturnsAsync((AppUser?)null);
        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

        var result = await _sut.Signup(new SignupDto("new@example.com", "weak", "New", "User"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        result.Error.Message.Should().Contain("Weak password");
    }

    [Test]
    public async Task Signup_Exitoso_CreaRolUserYGeneraToken() {
        _userManager.Setup(x => x.FindByEmailAsync("new@example.com")).ReturnsAsync((AppUser?)null);
        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), "USER"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.Signup(new SignupDto("new@example.com", "Password1!", "New", "User"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("USER");
        result.Value.Email.Should().Be("new@example.com");
        _userManager.Verify(x => x.AddToRoleAsync(
            It.Is<AppUser>(u => u.Email == "new@example.com"), "USER"), Times.Once);
        _emailService.Verify(service => service.SendEmailAsync(
            It.Is<EmailMessage>(message => message.To == "new@example.com")), Times.Once);
    }

    [Test]
    public async Task GetMe_UsuarioInexistente_DevuelveNotFound() {
        var result = await _sut.GetMe(123);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFound>();
    }

    [Test]
    public async Task GetMe_UsuarioExistente_DevuelveDatosYRol() {
        var user = NewUser();
        user.Id = 123;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _userManager.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == 123)))
            .ReturnsAsync(["ADMIN"]);

        var result = await _sut.GetMe(123);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(user.Email);
        result.Value.Nombre.Should().Be(user.Nombre);
        result.Value.Role.Should().Be("ADMIN");
    }

    [Test]
    public async Task GetMe_SinRol_UsaRolUser() {
        var user = NewUser();
        user.Id = 124;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _userManager.Setup(x => x.GetRolesAsync(It.Is<AppUser>(u => u.Id == 124)))
            .ReturnsAsync([]);

        var result = await _sut.GetMe(124);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("USER");
    }

    private static AppUser NewUser() => new() {
        Id = 42,
        UserName = "user@example.com",
        Email = "user@example.com",
        Nombre = "Test",
        Apellido = "User"
    };
}
