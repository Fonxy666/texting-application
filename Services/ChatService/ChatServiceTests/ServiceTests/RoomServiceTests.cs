using ChatService.Database;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Chat;
using ChatService.Repository.BaseRepository;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using ChatService.Services.Chat.RoomService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Textinger.Shared.Responses;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace ChatServiceTests.ServiceTests;

public class RoomServiceTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IRoomService _roomService;
    private readonly IRoomRepository _roomRepository;
    private readonly ChatContext _context;
    private readonly IConfiguration _configuration;
    private readonly Guid _testUserId = Guid.Parse("2f1b9e96-8c0b-4a4b-8fd3-9b4c0a447e31");
    private readonly Guid _existingUserId = Guid.Parse("3f3b1278-5c3e-4d51-842f-14d2a6581e2e");
    private readonly string _testConnectionString;
    
    
    public RoomServiceTests()
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
        
        services.AddLogging();
        
        services.AddDbContext<ChatContext>(options =>
            options.UseNpgsql(_testConnectionString));
        
        services.AddScoped<IUserGrpcService, FakeUserGrpcService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IBaseDatabaseRepository, BaseDatabaseRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        
        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<ChatContext>();
        _roomService = _provider.GetRequiredService<IRoomService>();
    }
    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
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
        var request = new RoomRequest("testRoomName123", "testPassword", "testKey");
        var result = await _roomService.RegisterRoomAsync(request, _testUserId);
        
        Assert.That(result, Is.InstanceOf<SuccessWithDto<RoomResponseDto>>());
        
        //failed test when user not existing
        var failedUserIdRequest = await _roomService.RegisterRoomAsync(request, Guid.NewGuid());

        Assert.That(failedUserIdRequest, Is.EqualTo(new FailureWithMessage("User not existing.")));
        
        //failed test witht he same room
        var roomNotExistingResult = await _roomService.RegisterRoomAsync(request, _testUserId);

        Assert.That(roomNotExistingResult, Is.EqualTo(new FailureWithMessage("This room already exists.")));
    }
    
    [Fact]
    public async Task DeleteRoom_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        // init test room
        var roomRequest = new RoomRequest("testRoomName123", "testPassword", "testKey");
        await _roomService.RegisterRoomAsync(roomRequest, _testUserId);
        
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomRequest.RoomName);
        
        //failed test when user not existing
        var failedUserNotExistingIdRequest = await _roomService.DeleteRoomAsync(Guid.NewGuid(), room.RoomId);

        Assert.That(failedUserNotExistingIdRequest, Is.EqualTo(new FailureWithMessage("User not existing.")));
        
        //failed test with not the creator id
        var roomNotExistingResult = await _roomService.DeleteRoomAsync(_existingUserId, room.RoomId);

        Assert.That(roomNotExistingResult, Is.EqualTo(new FailureWithMessage("You don't have permission to delete this room.")));
        
        // success test
        var result = await _roomService.DeleteRoomAsync(_testUserId, room.RoomId);
        
        Assert.That(result, Is.InstanceOf<Success>());
        
        //failed test when room not existing
        var failedUserIdRequest = await _roomService.DeleteRoomAsync(_testUserId, room.RoomId);

        Assert.That(failedUserIdRequest, Is.EqualTo(new FailureWithMessage("Room not found.")));
    }
    
    [Fact]
    public async Task ChangeRoomPassword_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        // init test room
        var roomRequest = new RoomRequest("testRoomName123", "testPassword", "testKey");
        await _roomService.RegisterRoomAsync(roomRequest, _testUserId);
        
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomRequest.RoomName);
        
        //success test
        var request = new ChangeRoomPassword(room.RoomId, "testPassword", "newTestPassword");
        var result = await _roomService.ChangePasswordAsync(request, _testUserId);
        
        Assert.That(result, Is.InstanceOf<Success>());
        
        // failure with not existing room
        var roomNotExistingRequest = new ChangeRoomPassword(Guid.NewGuid(), "newTestPassword", "newTestPassword123");
        var failureResultWithNotExistingRoom = await _roomService.ChangePasswordAsync(roomNotExistingRequest, _testUserId);
        
        Assert.That(failureResultWithNotExistingRoom, Is.EqualTo(new FailureWithMessage("Room not found.")));
        
        //failed test with wrong password
        var wrongPasswordRequest = new ChangeRoomPassword(room.RoomId, "testPassword", "newTestPassword");
        var failureWithWrongPasswordResult = await _roomService.ChangePasswordAsync(wrongPasswordRequest, _testUserId);
        
        Assert.That(failureWithWrongPasswordResult, Is.EqualTo(new FailureWithMessage("Wrong password.")));
        
        //failed test with not the creator tries to change
        var updatedRequest = new ChangeRoomPassword(room.RoomId, "newTestPassword", "newTestPassword123");
        var failureResultWithNotTheCreatorId = await _roomService.ChangePasswordAsync(updatedRequest, _existingUserId);
        
        Assert.That(failureResultWithNotTheCreatorId, Is.EqualTo(new FailureWithMessage("You don't have permission to change this room's password.")));
        
        //failed test with not the existing tries to change
        var failureResultWithNotExistingId = await _roomService.ChangePasswordAsync(updatedRequest, Guid.NewGuid());
        
        Assert.That(failureResultWithNotExistingId, Is.EqualTo(new FailureWithMessage("User not existing.")));
    }
    
    [Fact]
    public async Task UserIsTheCreator_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        // init test room
        var roomRequest = new RoomRequest("testRoomName123", "testPassword", "testKey");
        await _roomService.RegisterRoomAsync(roomRequest, _testUserId);
        
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomRequest.RoomName);
        
        //success test
        var result = await _roomService.UserIsTheCreatorAsync(_testUserId, room.RoomId);
        
        Assert.That(result, Is.True);
        
        // failure 
        var failureResult = await _roomService.UserIsTheCreatorAsync(Guid.NewGuid(), room.RoomId);
        
        Assert.That(failureResult, Is.False);
    }
    
    [Fact]
    public async Task RoomLogin_HandlesValidMessageSend_AndPreventEdgeCases()
    {
        // init test room
        var roomRequest = new RoomRequest("testRoomName123", "testPassword", "testKey");
        await _roomService.RegisterRoomAsync(roomRequest, _testUserId);
        
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomName == roomRequest.RoomName);
        
        //success test
        var request = new JoinRoomRequest(roomRequest.RoomName, roomRequest.Password);
        var result = await _roomService.LoginAsync(request, _testUserId);
        
        Assert.That(result, Is.InstanceOf<SuccessWithDto<RoomResponseDto>>());
        
        // failure with not existing room
        var notExistingRoomNameRequest = new JoinRoomRequest("failureRoomName", roomRequest.Password);
        var failureResultWithNotExistingRoomName = await _roomService.LoginAsync(notExistingRoomNameRequest, _testUserId);
        
        Assert.That(failureResultWithNotExistingRoomName, Is.EqualTo(new FailureWithMessage("Room not found.")));
        
        //failed test with wrong password
        var wrongPasswordRequest = new JoinRoomRequest(roomRequest.RoomName, "failurePassword");
        var failureResultWithWrongPassword = await _roomService.LoginAsync(wrongPasswordRequest, _testUserId);
        
        Assert.That(failureResultWithWrongPassword, Is.EqualTo(new FailureWithMessage("Wrong password.")));
    }
}