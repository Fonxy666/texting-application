using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;
using Server;
using Server.Hub;
using Server.Model;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Message;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.HubTests;

public class ChatHubTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly IDictionary<string, UserRoomConnection> _connection;
    private readonly ChatHub _chatHub;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public ChatHubTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _contextMock = new Mock<HubCallerContext>();
        _groupsMock = new Mock<IGroupManager>();

        _connection = new Dictionary<string, UserRoomConnection>();
        _chatHub = new ChatHub(_connection, _userManagerMock.Object)
        {
            Clients = _clientsMock.Object,
            Context = _contextMock.Object,
            Groups = _groupsMock.Object
        };
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(
                    new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("testConfiguration.json")
                        .Build()
                );
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
    }

    [Fact]
    public async Task JoinRoom_ShouldAddUserToGroupAndNotify()
    {
        var userConnection = new UserRoomConnection("testUser", "testRoom");
        const string connectionId = "test-connection-id";

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _groupsMock.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var applicationUser = new ApplicationUser { UserName = "testUser", Id = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") };
        var users = new List<ApplicationUser> { applicationUser }.AsQueryable().BuildMock();

        _userManagerMock.Setup(um => um.Users).Returns(users);

        var result = await _chatHub.JoinRoom(userConnection);

        Assert.Equal(connectionId, result);
        Assert.Contains(connectionId, _connection.Keys);
        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), default), Times.Once);
    }
    
    [Fact]
    public async Task SendMessage_ShouldSendToGroup()
    {
        var cookies = await TestLogin.Login_With_Test_User(new AuthRequest("TestUsername1", "testUserPassword123###"), _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var request = new MessageRequest("room1", "test message", false, null);
        const string connectionId = "test-connection-id";
        var userRoomConnection = new UserRoomConnection("testUser", "testRoom");

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _connection[connectionId] = userRoomConnection;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _contextMock.Setup(c => c.User).Returns(claimsPrincipal);

        await _chatHub.SendMessage(request);

        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task ModifyMessage_ShouldSendModifyMessageToGroup()
    {
        var request = new EditMessageRequest(new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a9"), "Updated message");
        const string connectionId = "test-connection-id";
        var userRoomConnection = new UserRoomConnection("testUser", "testRoom");

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _connection[connectionId] = userRoomConnection;

        await _chatHub.ModifyMessage(request);

        _clientProxyMock.Verify(c => c.SendCoreAsync("ModifyMessage", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task ModifyMessageSeen_ShouldSendModifyMessageSeenToGroup()
    {
        var request = new MessageSeenRequest("user1");
        const string connectionId = "test-connection-id";
        var userRoomConnection = new UserRoomConnection("testUser", "testRoom");

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _connection[connectionId] = userRoomConnection;

        await _chatHub.ModifyMessageSeen(request);

        _clientProxyMock.Verify(c => c.SendCoreAsync("ModifyMessageSeen", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task DeleteMessage_ShouldSendDeleteMessageToGroup()
    {
        const string messageId = "msg1";
        const string connectionId = "test-connection-id";
        var userRoomConnection = new UserRoomConnection("testUser", "testRoom");

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _connection[connectionId] = userRoomConnection;

        await _chatHub.DeleteMessage(messageId);

        _clientProxyMock.Verify(c => c.SendCoreAsync("DeleteMessage", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveUserFromGroupAndNotify()
    {
        const string connectionId = "test-connection-id";
        var userRoomConnection = new UserRoomConnection("testUser", "testRoom");

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _connection[connectionId] = userRoomConnection;

        await _chatHub.OnDisconnectedAsync(null);

        Assert.DoesNotContain(connectionId, _connection.Keys);
        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), default), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync("UserDisconnected", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task SendConnectedUser_ShouldSendConnectedUserListToGroup()
    {
        const string room = "testRoom";
        const string connectionId1 = "test-connection-id-1";
        const string connectionId2 = "test-connection-id-2";

        var userRoomConnection1 = new UserRoomConnection("testUser1", room);
        var userRoomConnection2 = new UserRoomConnection("testUser2", room);

        _connection[connectionId1] = userRoomConnection1;
        _connection[connectionId2] = userRoomConnection2;

        var applicationUser1 = new ApplicationUser { UserName = "testUser1", Id = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") };
        var applicationUser2 = new ApplicationUser { UserName = "testUser2", Id = new Guid("a7a1bb36-94ef-4a92-98ea-87ef49d5043a") };

        var users = new List<ApplicationUser> { applicationUser1, applicationUser2 }.AsQueryable().BuildMock();

        _userManagerMock.Setup(um => um.Users).Returns(users);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        await _chatHub.SendConnectedUser(room);

        _clientProxyMock.Verify(c => c.SendCoreAsync("ConnectedUser", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task OnRoomDelete_ShouldNotifyRoomDeletionAndRemoveFromGroup()
    {
        const string roomId1 = "room1";
        const string connectionId1 = "test-connection-id-1";

        var userRoomConnection1 = new UserRoomConnection("testUser1", roomId1);

        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId1);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _groupsMock.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        _connection[connectionId1] = userRoomConnection1;

        await _groupsMock.Object.AddToGroupAsync(connectionId1, roomId1);

        await _chatHub.OnRoomDelete(roomId1);

        Assert.DoesNotContain(connectionId1, _connection.Keys);

        _clientProxyMock.Verify(c => c.SendCoreAsync("RoomDeleted", It.Is<object[]>(o => o.Contains(roomId1)), default), Times.Exactly(2));
    }
}