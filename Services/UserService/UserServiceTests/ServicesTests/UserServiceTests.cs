using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Helpers;
using UserService.Models;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.FriendConnectionService;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.User;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace UserServiceTests.ServicesTests;

public class UserServiceTest : IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ServiceProvider _provider;
    private readonly IApplicationUserService _userService;
    private readonly IFriendConnectionService _friendConnectionService;
    private readonly MainDatabaseContext _context;
    private readonly IConfiguration _configuration;

    public UserServiceTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var services = new ServiceCollection();

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();

        services.AddDbContext<MainDatabaseContext>(options =>
            options.UseNpgsql(
                "Host=localhost;Port=5434;Username=postgres;Password=testPassword123@;Database=test_user_db;SSL Mode=Disable;"));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDatabaseContext>();

        services.AddSingleton(_configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPrivateKeyService, FakeKeyService>();
        services.AddScoped<ICookieService, FakeCookieService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IUserHelper, UserHelper>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _friendConnectionService =  _provider.GetRequiredService<IFriendConnectionService>();
        _userService = _provider.GetRequiredService<IApplicationUserService>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        var app = _provider.GetRequiredService<IApplicationBuilder>();
        PopulateDbAndAddRoles.AddRolesAndAdminSync(app, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(app, 2);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task GetUsernameAsync_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");

        var successResult = await _userService.GetUserNameAsync(userIdForSuccess);

        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserNameDto>>());
        
        // Failed test
        var userIdForNotFound = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");

        var notFoundResult = await _userService.GetUserNameAsync(userIdForNotFound);

        Assert.That(notFoundResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }

    [Fact]
    public async Task GetImageAsync_HandlesValidRequest_AndPreventsEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");

        var successResult = await _userService.GetImageWithIdAsync(userIdForSuccess);

        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<ImageDto>>());
        
        // User not existing test
        var userIdForNotExistingUserFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");

        var notExistingUserFailureResult = await _userService.GetImageWithIdAsync(userIdForNotExistingUserFailure);

        Assert.That(notExistingUserFailureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
        
        // Image not existing test
        var userIdForNotExistingImageFailure = Guid.Parse("10f96e12-e245-420a-8bad-b61fb21c4b2d");

        var notExistingImageFailureResult = await _userService.GetImageWithIdAsync(userIdForNotExistingImageFailure);

        Assert.That(notExistingImageFailureResult, Is.EqualTo(new FailureWithMessage("User image not found.")));
    }

    [Fact]
    public async Task GetUserCredentialsAsync_HandlesValidRequesst_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");

        var successResult = await _userService.GetUserCredentialsAsync(userIdForSuccess);

        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UsernameUserEmailAndTwoFactorEnabledDto>>());
        
        // User not existing test
        var userIdForNotExistingUserFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");

        var notExistingUserFailureResult = await _userService.GetUserCredentialsAsync(userIdForNotExistingUserFailure);

        Assert.That(notExistingUserFailureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }
    
    [Fact]
    public async Task GetUserWithSentRequestsAsync_HandlesValidRequesst_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.GetUserWithSentRequestsAsync(userIdForSuccess) as SuccessWithDto<ApplicationUser>;
        Assert.That(0, Is.EqualTo(successResult.Data.SentFriendRequests.Count));
        
        await _friendConnectionService.SendFriendRequestAsync(userIdForSuccess, "TestUsername2");
        // After a friend request sent, the value will be +1
        var successResultAfterFriendSend = await _userService.GetUserWithSentRequestsAsync(userIdForSuccess) as SuccessWithDto<ApplicationUser>;
        Assert.That(1, Is.EqualTo(successResultAfterFriendSend.Data.SentFriendRequests.Count));
        
        // Failure, not existing user
        var userIdForFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureResult = await _userService.GetUserWithSentRequestsAsync(userIdForFailure);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }
    
    [Fact]
    public async Task GetUserWithReceivedRequestsAsync_HandlesValidRequesst_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.GetUserWithReceivedRequestsAsync(userIdForSuccess) as SuccessWithDto<ApplicationUser>;
        Assert.That(0, Is.EqualTo(successResult.Data.ReceivedFriendRequests.Count));
        
        await _friendConnectionService.SendFriendRequestAsync(new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"), "TestUsername1");
        // After a friend request sent, the value will be +1
        var successResultAfterFriendSend = await _userService.GetUserWithReceivedRequestsAsync(userIdForSuccess) as SuccessWithDto<ApplicationUser>;
        Assert.That(1, Is.EqualTo(successResultAfterFriendSend.Data.ReceivedFriendRequests.Count));
        
        // Failure, not existing user
        var userIdForFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureResult = await _userService.GetUserWithReceivedRequestsAsync(userIdForFailure);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }
}