using ChatService.Hub;
using ChatService.Model;
using ChatService.Model.Responses.Chat;
using ChatService.Services.Chat.GrpcService;
using ChatServiceTests;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using Assert = NUnit.Framework.Assert;

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
        var roomId = Guid.NewGuid().ToString();
        var connectionId = "conn-123";
        var userConnection = new UserRoomConnection("TestUserName1", roomId);

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockClients.Setup(c => c.Group(roomId)).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

        await _hub.JoinRoom(userConnection);

        Assert.True(_connectionStore.ContainsKey(connectionId));
        Assert.That(_connectionStore[connectionId], Is.EqualTo(userConnection));
        
        _mockClientProxy.Verify(c => c.SendCoreAsync("ReceiveMessage",
            It.Is<object[]>(args => MatchReceiveMessageResponseForBot(args, $"{userConnection.User} has joined the room!", Guid.Parse(roomId))),
            default), Times.Once);
    }
    
    private bool MatchReceiveMessageResponseForBot(object[] args, string expectedText, Guid expectedRoomId)
    {
        var res = args[0] as ReceiveMessageResponseForBot;
        return res != null && res.Text == expectedText && res.RoomId == expectedRoomId;
    }
}