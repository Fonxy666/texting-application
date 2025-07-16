using ChatService.Database;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Message;
using ChatService.Repository.MessageRepository;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using ChatService.Services.Chat.MessageService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Textinger.Shared.Responses;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace ChatServiceTests.ServiceTests;

public class MessageServiceTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IMessageService _messageService;
    private readonly IRoomRepository _roomRepository;
    private readonly ChatContext _context;
    private readonly IConfiguration _configuration;
    private readonly Guid _testUserId = Guid.Parse("2f1b9e96-8c0b-4a4b-8fd3-9b4c0a447e31");
    private readonly string _testConnectionString;
    
    public MessageServiceTests()
    {
        var services = new ServiceCollection();
        
        var baseConfig  = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();
        
        _testConnectionString = baseConfig["TestConnectionString"]!;
        
        _configuration = new ConfigurationBuilder()
            .AddConfiguration(baseConfig)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", _testConnectionString }
            }!)
            .Build();
        
        services.AddDbContext<ChatContext>(options =>
            options.UseNpgsql(_testConnectionString));
        
        services.AddScoped<IUserGrpcService, FakeUserGrpcService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        
        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<ChatContext>();
        _messageService = _provider.GetRequiredService<IMessageService>();
        _roomRepository = _provider.GetRequiredService<IRoomRepository>();
    }
    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        await PopulateDb.CreateTestRoomsAsync(_provider, 3);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SendMessage_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        // success test
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var request = new MessageRequest(room.RoomId, "testMessage", false, "testIv", null);
        var result = await _messageService.SendMessage(request, _testUserId);
        
        Assert.That(result, Is.InstanceOf<SuccessWithDto<Message>>());
        
        //failed test when user not existing
        var failedUserIdResult = await _messageService.SendMessage(request, Guid.NewGuid());

        Assert.That(failedUserIdResult, Is.EqualTo(new FailureWithMessage("User not found.")));
        
        //failed test when room not existing
        var failedRequest = new MessageRequest(Guid.NewGuid(), "testMessage", false, "testIv", null);
        var roomNotExistingResult = await _messageService.SendMessage(failedRequest, _testUserId);

        Assert.That(roomNotExistingResult, Is.EqualTo(new FailureWithMessage("Room not found.")));
    }
    
    [Fact]
    public async Task GetMessages_HandlesValidRequest_AndPreventEdgeCases()
    {
        // success test
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var request = new GetMessagesRequest(room.RoomId, 1);
        var result = await _messageService.GetLast10Messages(request);
        
        Assert.That(result, Is.InstanceOf<SuccessWithDto<IList<MessageDto>>>());
        
        //failed test when room not existing
        var failedRequest = new GetMessagesRequest(Guid.NewGuid(), 1);
        var failedUserIdResult = await _messageService.GetLast10Messages(failedRequest);

        Assert.That(failedUserIdResult, Is.EqualTo(new FailureWithMessage("There is no room with the given id.")));
    }

    [Fact]
    public async Task EditMessage_HandlesValidRequest_AndPreventEdgeCases()
    {
        // init message
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var sendMessage = new MessageRequest(room.RoomId, "testMessage", false, "testIv", null);
        await _messageService.SendMessage(sendMessage, _testUserId);

        var message = await _context.Messages.FirstOrDefaultAsync(m => m.SenderId == _testUserId);
        
        // success test
        var request = new EditMessageRequest(message.MessageId, "editTestMessage", "testIv");
        var result = await _messageService.EditMessage(request, _testUserId);

        Assert.That(result, Is.InstanceOf<Success>());

        //failed test when message not existing
        var failedRequestWithWrongId = new EditMessageRequest(Guid.NewGuid(), "editTestMessage", "testIv");
        var failedMessageIdResult = await _messageService.EditMessage(failedRequestWithWrongId, _testUserId);

        Assert.That(failedMessageIdResult,
            Is.EqualTo(new FailureWithMessage("There is no message with the given id.")));

        //failed test when user is not the sender
        var failedRequestWithNotSender = await _messageService.EditMessage(request, Guid.NewGuid());

        Assert.That(failedRequestWithNotSender, Is.EqualTo(new FailureWithMessage("You don't have permission.")));
    }
    
    [Fact]
    public async Task EditMessageSeen_HandlesValidRequest_AndPreventEdgeCases()
    {
        // init message
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var sendMessage = new MessageRequest(room.RoomId, "testMessage", false, "testIv", null);
        await _messageService.SendMessage(sendMessage, _testUserId);

        var message = await _context.Messages.FirstOrDefaultAsync(m => m.SenderId == _testUserId);
        
        // success test
        var request = new EditMessageSeenRequest(message.MessageId);
        var result = await _messageService.EditMessageSeen(request, _testUserId);

        Assert.That(result, Is.InstanceOf<Success>());

        //failed test when message not existing
        var failedRequestWithWrongId = new EditMessageSeenRequest(Guid.NewGuid());
        var failedMessageIdResult = await _messageService.EditMessageSeen(failedRequestWithWrongId, _testUserId);

        Assert.That(failedMessageIdResult,
            Is.EqualTo(new FailureWithMessage("There is no message with the given id.")));
    }
    
    [Fact]
    public async Task DeleteMessage_HandlesValidRequest_AndPreventEdgeCases()
    {
        // init message
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var sendMessage = new MessageRequest(room.RoomId, "testMessage", false, "testIv", null);
        await _messageService.SendMessage(sendMessage, _testUserId);

        var message = await _context.Messages.FirstOrDefaultAsync(m => m.SenderId == _testUserId);

        //failed test when message not existing
        var failedMessageIdResult = await _messageService.DeleteMessage(Guid.NewGuid(), _testUserId);

        Assert.That(failedMessageIdResult,
            Is.EqualTo(new FailureWithMessage("There is no message with the given id.")));
        
        //failed test when no permission
        var failedRequestWithNotTheSender = await _messageService.DeleteMessage(message.MessageId, Guid.NewGuid());

        Assert.That(failedRequestWithNotTheSender,
            Is.EqualTo(new FailureWithMessage("You don't have permission.")));
        
        // success test
        var result = await _messageService.DeleteMessage(message.MessageId, _testUserId);

        Assert.That(result, Is.InstanceOf<Success>());
    }
}