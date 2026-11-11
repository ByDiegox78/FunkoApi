using System.Security.Claims;
using FluentAssertions;
using FunkoApiV._2.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FunkoApiV2Test.Unit.Realtime;

[TestFixture]
[Category("Unit")]
public class FunkosHubTests {
    [Test]
    public async Task OnConnectedAsync_usuarioAdmin_seUneAlGrupoPersonalYAdmins() {
        // Arrange
        var (hub, groups) = CreateHub(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, "ADMIN")
        ], "test")));

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(manager => manager.AddToGroupAsync(
            "connection-1", "user-user-1", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(manager => manager.AddToGroupAsync(
            "connection-1", "admins", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_usuarioSinRolAdmin_soloSeUneAlGrupoPersonal() {
        // Arrange
        var (hub, groups) = CreateHub(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-2"),
            new Claim(ClaimTypes.Role, "USER")
        ], "test")));

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(manager => manager.AddToGroupAsync(
            "connection-1", "user-user-2", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(manager => manager.AddToGroupAsync(
            It.IsAny<string>(), "admins", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_sinIdentidad_noSeUneANingunGrupo() {
        // Arrange
        var (hub, groups) = CreateHub(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(manager => manager.AddToGroupAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnDisconnectedAsync_conExcepcionCompletaLaDesconexion() {
        // Arrange
        var (hub, _) = CreateHub(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        var act = () => hub.OnDisconnectedAsync(new InvalidOperationException("desconexión"));

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SendMessage_enviaMensajeATodosLosClientes() {
        // Arrange
        var (hub, _, clients, clientProxy) = CreateHubWithClients();

        // Act
        await hub.SendMessage("hola");

        // Assert
        clients.VerifyGet(provider => provider.All, Times.Once);
        clientProxy.Verify(proxy => proxy.SendCoreAsync(
            "ReceiveMessage", It.Is<object?[]>(args => (string)args[0]! == "hola"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (FunkosHub Hub, Mock<IGroupManager> Groups) CreateHub(ClaimsPrincipal user) {
        var (hub, groups, _, _) = CreateHubWithClients();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("connection-1");
        context.SetupGet(caller => caller.User).Returns(user);
        hub.Context = context.Object;
        return (hub, groups);
    }

    private static (
        FunkosHub Hub,
        Mock<IGroupManager> Groups,
        Mock<IHubCallerClients> Clients,
        Mock<IClientProxy> ClientProxy) CreateHubWithClients() {
        var groups = new Mock<IGroupManager>();
        groups.Setup(manager => manager.AddToGroupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(provider => provider.All).Returns(clientProxy.Object);
        var hub = new FunkosHub(NullLogger<FunkosHub>.Instance) {
            Groups = groups.Object,
            Clients = clients.Object
        };
        return (hub, groups, clients, clientProxy);
    }
}
