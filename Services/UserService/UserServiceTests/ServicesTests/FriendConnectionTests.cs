using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Models;
using UserService.Models.Responses;
using UserService.Repository.AppUserRepository;
using UserService.Repository.FConnectionRepository;
using UserService.Services.Authentication;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.FriendConnectionService;
using UserService.Services.MediaService;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.User;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace UserServiceTests.ServicesTests;
public class FriendConnectionTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IFriendConnectionService _friendService;
    private readonly MainDatabaseContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _testConnectionString;

    public FriendConnectionTests()
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

        services.AddDbContext<MainDatabaseContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5434;Username=postgres;Password=testPassword123@;Database=test_user_db;SSL Mode=Disable;"));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDatabaseContext>();

        services.AddSingleton(_configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPrivateKeyService, FakeKeyService>();
        services.AddScoped<ICookieService, FakeCookieService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFriendConnectionRepository, FriendConnectionRepository>();
        
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _friendService = _provider.GetRequiredService<IFriendConnectionService>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        PopulateDbAndAddRoles.AddRolesAndAdminSync(_provider, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(_provider, 3, _context);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task SendFriendRequest_HandlesValidRequest_AndPreventsInvalidUserInvalidIdAlreadySentRequest()
    {
        // Success test
        var result =
            await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "TestUsername2");
        Assert.That(result, Is.InstanceOf<SuccessWithDto<ShowFriendRequestDto>>());
        
        // Bad request to yourself
        var notExistingUserResult =
            await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "TestUsername1");

        Assert.That(notExistingUserResult, Is.EqualTo(new FailureWithMessage("You cannot send friend request to yourself.")));
        
        // Bad request with not existing new friend
        var notExistingNewFriend =
            await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "NotExistingUser");

        Assert.That(notExistingNewFriend, Is.EqualTo(new FailureWithMessage("There is no User with this username: NotExistingUser")));
        
        // Sending request again not saving to db
        var replyResult =
            await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "TestUsername2");

        Assert.That(replyResult, Is.EqualTo(new FailureWithMessage("You already sent a friend request to this user!")));
    }
    
    [Fact]
    public async Task AcceptReceivedFriendRequest_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success test
        await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "TestUsername2");
        
        var requestId = _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") && fc.Receiver.UserName == "TestUsername2").Result.ConnectionId;
        
        var result = await _friendService.AcceptReceivedFriendRequestAsync(Guid.Parse("10f96e12-e245-420a-8bad-b61fb21c4b2d"), requestId);
        Assert.That(result, Is.InstanceOf<Success>());
        
        // Not existing requestId
        var notExistingRequest = await _friendService.AcceptReceivedFriendRequestAsync(Guid.Parse("10f96e12-e245-420a-8bad-b61fb21c4b2d"), Guid.NewGuid());
        Assert.That(notExistingRequest, Is.EqualTo(new FailureWithMessage("Request not found.")));
    }

    [Fact]
    public async Task GetPendingRequests_HandlesValidRequest()
    {
        var result = await _friendService.GetPendingRequestCountAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That(result, Is.InstanceOf<SuccessWithDto<NumberDto>>());
    }

    [Fact]
    public async Task DeleteFriendRequestAsync_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success delete if sender
        await _friendService.SendFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "TestUsername2");
        var sendRequestId = _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") && fc.Receiver.UserName == "TestUsername2").Result.ConnectionId;
        
        var sentDeleteResult = await _friendService.DeleteFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), UserType.Sender, sendRequestId);
        Assert.That(sentDeleteResult, Is.InstanceOf<Success>());
        
        // Success delete if receiver
        await _friendService.SendFriendRequestAsync(Guid.Parse("10f96e12-e245-420a-8bad-b61fb21c4b2d"), "TestUsername1");
        var receiverRequestId = _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("10f96e12-e245-420a-8bad-b61fb21c4b2d") && fc.Receiver.UserName == "TestUsername1").Result.ConnectionId;
        
        var receiverResult = await _friendService.DeleteFriendRequestAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), UserType.Receiver, receiverRequestId);
        Assert.That(receiverResult, Is.InstanceOf<Success>());
        
        // Failure: User not found
        var invalidUserId = Guid.NewGuid();
        var resultUserNotFound = await _friendService.DeleteFriendRequestAsync(invalidUserId, UserType.Sender, Guid.NewGuid());
        Assert.That(((FailureWithMessage)resultUserNotFound).Message, Is.EqualTo("User not found."));
        
        // Failure: Invalid user type
        var resultInvalidType = await _friendService.DeleteFriendRequestAsync(Guid.NewGuid(), (UserType)999, Guid.NewGuid());
        Assert.That(((FailureWithMessage)resultInvalidType).Message, Is.EqualTo("Invalid user type."));
        
        // Failure: Request not found
        var validUserId = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var nonExistentRequestId = Guid.NewGuid();
        var resultRequestNotFound = await _friendService.DeleteFriendRequestAsync(validUserId, UserType.Sender, nonExistentRequestId);
        Assert.That(((FailureWithMessage)resultRequestNotFound).Message, Is.EqualTo("Cannot find the request."));
    }
    
    [Fact]
    public async Task GetFriendsAsync_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success test
        var userId = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var existingUserBeforeNewFriend = await _friendService.GetFriendsAsync(userId) as SuccessWithDto<IList<ShowFriendRequestDto>>;
        
        Assert.That(existingUserBeforeNewFriend.Data.Count, Is.EqualTo(0));
        
        await _friendService.SendFriendRequestAsync(userId, "TestUsername2"); // send friend request
        var sendtRequest = await _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") && fc.Receiver!.UserName == "TestUsername2");
        await _friendService.AcceptReceivedFriendRequestAsync(userId, sendtRequest!.ConnectionId); // accept request
        
        var existingUserAfterNewFriend = await _friendService.GetFriendsAsync(userId) as SuccessWithDto<IList<ShowFriendRequestDto>>;
        Assert.That(existingUserAfterNewFriend!.Data!.Count, Is.EqualTo(1));
        
        var notExistingUser = await _friendService.GetFriendsAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915"));
        Assert.That(((FailureWithMessage)notExistingUser).Message, Is.EqualTo("User not found."));
    }
    
    [Fact]
    public async Task DeleteFriendAsync_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success test
        var userId = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        await _friendService.SendFriendRequestAsync(userId, "TestUsername2"); // send friend request
        var sentRequest = await _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") && fc.Receiver!.UserName == "TestUsername2");
        await _friendService.AcceptReceivedFriendRequestAsync(userId, sentRequest!.ConnectionId); // accept request
        
        var existingUserAfterNewFriend = await _friendService.GetFriendsAsync(userId) as SuccessWithDto<IList<ShowFriendRequestDto>>;
        Assert.That(existingUserAfterNewFriend!.Data!.Count, Is.EqualTo(1));

        var deleteFriendResponse = await _friendService.DeleteFriendAsync(userId, sentRequest!.ConnectionId);
        Assert.That(deleteFriendResponse, Is.InstanceOf<Success>());
        var existingUserAfterNewFriendDeletion = await _friendService.GetFriendsAsync(userId) as SuccessWithDto<IList<ShowFriendRequestDto>>;
        Assert.That(existingUserAfterNewFriendDeletion!.Data!.Count, Is.EqualTo(0));
        
        // Failure test
        var notExistingUser = await _friendService.DeleteFriendAsync(Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915"), sentRequest!.ConnectionId);
        Assert.That(((FailureWithMessage)notExistingUser).Message, Is.EqualTo("Cannot find friend connection."));
        
        // Failure no permission
        await _friendService.SendFriendRequestAsync(userId, "TestUsername2"); // send friend request
        var sentRequestAgain = await _context.FriendConnections.FirstOrDefaultAsync(fc => fc.SenderId == Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916") && fc.Receiver!.UserName == "TestUsername2");
        await _friendService.AcceptReceivedFriendRequestAsync(userId, sentRequestAgain!.ConnectionId); // accept request
        var noPermissionResult = await _friendService.DeleteFriendAsync(new Guid("995f04da-d4d3-447c-9c69-fab370bca312"), sentRequestAgain!.ConnectionId);
        Assert.That(((FailureWithMessage)noPermissionResult).Message, Is.EqualTo("You don't have permission for deletion."));
    }
}
