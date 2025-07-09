using ChatService.Database;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Repository.MessageRepository;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using ChatService.Services.Chat.MessageService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Textinger.Shared.Responses;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace ChatServiceTests.ServiceTests;

public class MessageServiceTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ServiceProvider _provider;
    private readonly IMessageService _messageService;
    private readonly IRoomRepository _roomRepository;
    private readonly ChatContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _testConnectionString;
    public MessageServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
        
        PopulateDb.CreateTestRooms(_provider, 3);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SendMessage_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        var room = await _roomRepository.GetRoomAsync("testRoomName1");
        var request = new MessageRequest(room.RoomId, "testMessage", false, "testIv", null);
        var result = await _messageService.SendMessage(request, Guid.NewGuid());
        
        Assert.That(result, Is.InstanceOf<SuccessWithDto<Message>>());
    }
}