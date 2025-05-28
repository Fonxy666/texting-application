using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UserService.Database;
using UserService.Hub;
using UserService.Models;
using UserService.Models.Requests;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace UserServiceTests.HubTests;

public class FriendRequestHubTests
{
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<ISingleClientProxy> _clientProxyMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly FriendRequestHub _friendRequestHub;
    private readonly Mock<MainDatabaseContext> _dbContextMock; 

    public FriendRequestHubTests()
    {
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<ISingleClientProxy>();
        _contextMock = new Mock<HubCallerContext>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

        var options = new DbContextOptionsBuilder<MainDatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // unique per test run
            .Options;

        var dbContext = new MainDatabaseContext(options); // Create real instance with in-memory DB

        _friendRequestHub = new FriendRequestHub(_userManagerMock.Object, dbContext)
        {
            Clients = _clientsMock.Object,
            Context = _contextMock.Object
        };
    }

    [Fact]
    public async Task SendFriendRequest_SendsRequestToReceiverAndSender()
    {
        var requestId = Guid.NewGuid().ToString();
        const string senderName = "TestUser";
        var senderId = Guid.NewGuid();
        var sentTime = DateTime.Now.ToString();
        const string receiverName = "testUser";
        var receiverId = Guid.NewGuid().ToString();

        _contextMock.SetupGet(c => c.ConnectionId).Returns("connection123");

        _clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        var applicationUser1 = new ApplicationUser { UserName = senderName, Id = senderId };
        var applicationUser2 = new ApplicationUser { UserName = receiverName, Id = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") };
        var users = new List<ApplicationUser> { applicationUser1, applicationUser2 }.AsQueryable().BuildMock();

        _userManagerMock.Setup(um => um.Users).Returns(users);
        _userManagerMock.Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => users.FirstOrDefault(u => u.UserName == name));

        var testConnections = new ConcurrentDictionary<string, string>();
        testConnections.TryAdd(applicationUser1.Id.ToString(), "senderConnectionId");
        testConnections.TryAdd(applicationUser2.Id.ToString(), "receiverConnectionId");
        FriendRequestHub.Connections = testConnections;

        var request = new ManageFriendRequest(requestId, senderName, senderId.ToString(), sentTime, receiverName, receiverId);
        await _friendRequestHub.SendFriendRequest(request);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "ReceiveFriendRequest",
                It.Is<object[]>(o => o.Length == 1 && o[0].Equals(request)),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
    
    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionFromDictionary()
    {
        const string connectionId = "connection123";
        _contextMock.SetupGet(c => c.ConnectionId).Returns(connectionId);
        FriendRequestHub.Connections["user123"] = connectionId;

        await _friendRequestHub.OnDisconnectedAsync(null);

        Assert.False(FriendRequestHub.Connections.ContainsKey("user123"));
    }
    
    [Fact]
    public async Task JoinToHub_AddsUserToConnectionsDictionary()
    {
        var userId = Guid.NewGuid();
        _contextMock.SetupGet(c => c.ConnectionId).Returns("connection123");

        await _friendRequestHub.JoinToHub(userId.ToString());

        Assert.AreEqual("connection123", FriendRequestHub.Connections[userId.ToString()]);
    }
    
    [Fact]
    public async Task AcceptFriendRequest_SendsAccepted()
    {
        var requestId = Guid.NewGuid().ToString();
        const string senderName = "TestUser";
        var senderId = Guid.NewGuid().ToString();
        var sentTime = DateTime.Now.ToString();
        const string receiverName = "testUser";
        var receiverId = Guid.NewGuid().ToString();

        _contextMock.SetupGet(c => c.ConnectionId).Returns("connection123");

        _clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        var applicationUser1 = new ApplicationUser { UserName = senderName, Id = Guid.Parse(senderId) };
        var applicationUser2 = new ApplicationUser { UserName = receiverName, Id = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") };
        var users = new List<ApplicationUser> { applicationUser1, applicationUser2 }.AsQueryable().BuildMock();

        _userManagerMock.Setup(um => um.Users).Returns(users);
        _userManagerMock.Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => users.FirstOrDefault(u => u.UserName == name));

        var testConnections = new ConcurrentDictionary<string, string>();
        testConnections.TryAdd(senderId, "senderConnectionId");
        FriendRequestHub.Connections = testConnections;

        var request = new ManageFriendRequest(requestId, senderName, senderId, sentTime, receiverName, receiverId);
        await _friendRequestHub.AcceptFriendRequest(request);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "AcceptFriendRequest",
                It.Is<object[]>(o => o.Length == 1 && o[0] is ManageFriendRequest),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task DeleteFriendRequest_SendsDeleteToSenderAndReceiver()
    {
        var requestId = Guid.NewGuid().ToString();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var testConnections = new ConcurrentDictionary<string, string>();
        testConnections.TryAdd(senderId.ToString(), "senderConnectionId");
        testConnections.TryAdd(receiverId.ToString(), "receiverConnectionId");
        FriendRequestHub.Connections = testConnections;

        _clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        var request = new DeleteFriendRequest(requestId, senderId.ToString(), receiverId.ToString());
        await _friendRequestHub.DeleteFriendRequest(request);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync("DeleteFriendRequest",
                It.Is<object[]>(o =>
                    o != null &&
                    o.Length == 1 &&
                    o[0].ToString() == requestId),
                default),
            Times.Exactly(2));
    }
    
    [Fact]
    public async Task DeleteFriend_SendsDeleteToSenderAndReceiver()
    {
        var requestId = Guid.NewGuid().ToString();
        var senderId = Guid.NewGuid().ToString();
        var receiverId = Guid.NewGuid().ToString();

        _clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        var testConnections = new ConcurrentDictionary<string, string>();
        testConnections.TryAdd(senderId, "senderConnectionId");
        testConnections.TryAdd(receiverId, "receiverConnectionId");
        FriendRequestHub.Connections = testConnections;

        var request = new DeleteFriendRequest(requestId, senderId, receiverId);
        await _friendRequestHub.DeleteFriend(request);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync("DeleteFriend",
                It.Is<object[]>(o =>
                    o != null &&
                    o.Length == 1 &&
                    o[0].ToString() == requestId),
                default),
            Times.Exactly(2));
    }
}