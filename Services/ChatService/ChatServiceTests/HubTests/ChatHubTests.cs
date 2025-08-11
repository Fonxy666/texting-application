using System.Security.Claims;
using ChatService.Hub;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Chat;
using ChatService.Services.Chat.GrpcService;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace ChatServiceTests.HubTests;

public class ChatHubTests
{
    private readonly IUserGrpcService _fakeUserGrpcService;
    private readonly Dictionary<string, UserRoomConnection> _connectionStore;
    private readonly ChatHub _hub;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IGroupManager> _mockGroupManager;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockContext;

    public ChatHubTests()
    {
        _fakeUserGrpcService = new FakeUserGrpcService();
        
        _connectionStore = new Dictionary<string, UserRoomConnection>();
        _hub = new ChatHub(_connectionStore, _fakeUserGrpcService);

        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroupManager = new Mock<IGroupManager>();

        _hub.Clients = _mockClients.Object;
        _hub.Context = _mockContext.Object;
        _hub.Groups = _mockGroupManager.Object;

        _mockGroupManager.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task JoinRoom_ShouldAddUserAndSendBotMessage()
    {
        var roomId = Guid.NewGuid();
        var connectionId = "conn-123";
        var userConnection = new UserRoomConnection("TestUserName1", roomId.ToString());

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockClients.Setup(c => c.Group(roomId.ToString())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

        await _hub.JoinRoom(userConnection);

        Assert.True(_connectionStore.ContainsKey(connectionId));
        Assert.That(_connectionStore[connectionId], Is.EqualTo(userConnection));
        
        _mockClientProxy.Verify(c => c.SendCoreAsync("ReceiveMessage",
            It.Is<object[]>(args => MatchResponse<ReceiveMessageResponseForBot>(args, $"{userConnection.User} has joined the room!", roomId)),
            default), Times.Once);
    }
    
    [Fact]
    public void GetConnectedUsers_ShouldReturnCorrectCount()
    {
        var roomId = "room-123";
        _connectionStore["conn1"] = new UserRoomConnection("User1", roomId);
        _connectionStore["conn2"] = new UserRoomConnection("User2", roomId);
        _connectionStore["conn3"] = new UserRoomConnection("User3", "other-room");

        var count = _hub.GetConnectedUsers(roomId);

        Assert.That(count, Is.EqualTo(2));
    }
    
    [Fact]
    public async Task SendMessage_ShouldBroadcastToGroup()
    {
        const string connId = "conn-200";
        var roomId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();

        _mockContext.Setup(c => c.ConnectionId).Returns(connId);
        _hub.Context = _mockContext.Object;
        _hub.Clients = _mockClients.Object;
        _connectionStore[connId] = new UserRoomConnection("User1", roomId.ToString());

        _mockClients.Setup(c => c.Group(roomId.ToString())).Returns(_mockClientProxy.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _mockContext.Setup(c => c.User).Returns(principal);

        var request = new MessageRequest(roomId, "Hello", false, "sample-iv", messageId);

        await _hub.SendMessage(request);

        _mockClientProxy.Verify(proxy => proxy.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object[]>(args => MatchResponse<ReceiveMessageResponse>(args, "Hello", null)),
            default), Times.Once);
    }
    
    [Fact]
    public async Task ModifyMessage_ShouldBroadcastEdit()
    {
        const string connId = "conn-200";
        var roomId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid();
        _connectionStore[connId] = new UserRoomConnection("UserX", roomId);
        _mockContext.Setup(c => c.ConnectionId).Returns(connId);
        _mockClients.Setup(c => c.Group(roomId)).Returns(_mockClientProxy.Object);

        var request = new EditMessageRequest(messageId, "edited", "iv-123");
        _hub.Context = _mockContext.Object;
        _hub.Clients = _mockClients.Object;

        await _hub.ModifyMessage(request);
        
        _mockClientProxy.Verify(proxy => proxy.SendCoreAsync(
            "ModifyMessage",
            It.Is<object[]>(args => MatchResponse<EditMessageResponse>(args, "edited", null)),
            default), Times.Once);
    }

    [Fact]
    public async Task ModifyMessageSeen_ShouldNotifyGroup()
    {
        const string connId = "conn-200";
        var roomId = Guid.NewGuid().ToString();
        _connectionStore[connId] = new UserRoomConnection("UserSeen", roomId);

        _mockContext.Setup(c => c.ConnectionId).Returns(connId);
        _hub.Context = _mockContext.Object;
        _mockClients.Setup(c => c.Group(roomId)).Returns(_mockClientProxy.Object);

        var request = new MessageSeenRequest(Guid.NewGuid());

        await _hub.ModifyMessageSeen(request);

        _mockClientProxy.Verify(p => p.SendCoreAsync("ModifyMessageSeen",
            It.Is<object[]>(args => MatchGuidPayload(args, request.UserId)), default), Times.Once);
    }
    
    [Fact]
    public async Task DeleteMessage_ShouldNotifyGroup()
    {
        const string connId = "conn-200";
        var roomId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();

        _connectionStore[connId] = new UserRoomConnection("UserDel", roomId);
        _mockContext.Setup(c => c.ConnectionId).Returns(connId);
        _mockClients.Setup(c => c.Group(roomId)).Returns(_mockClientProxy.Object);
        _hub.Context = _mockContext.Object;
        _hub.Clients = _mockClients.Object;

        await _hub.DeleteMessage(messageId);

        _mockClientProxy.Verify(proxy => proxy.SendCoreAsync("DeleteMessage",
            It.Is<object[]>(args => args[0] as string == messageId), default));
    }
    
    private bool MatchGuidPayload(object[] args, Guid expectedUserId)
    {
        return args.Length == 1 && args[0] is Guid guid && guid == expectedUserId;
    }
    
    private bool MatchResponse<T>(object[] args, string expectedText, Guid? expectedRoomId) where T : class
    {
        if (args[0] is T res)
        {
            var textProp = typeof(T).GetProperty("Text");
            var roomIdProp = typeof(T).GetProperty("RoomId");

            var textValue = textProp?.GetValue(res) as string;
            var roomIdValue = roomIdProp?.GetValue(res);

            if (expectedRoomId != null && roomIdValue is Guid actualRoomId)
            {
                return textValue == expectedText && actualRoomId == expectedRoomId.Value;
            }

            return textValue == expectedText;
        }
        return false;
    }
}