using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Repository.AppUserRepository;
using UserService.Repository.BaseDbRepository;
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

public class UserServiceTest : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IApplicationUserService _userService;
    private readonly IFriendConnectionService _friendConnectionService;
    private readonly MainDatabaseContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private int testUserNumber = 2;
    private readonly string _testConnectionString;

    public UserServiceTest()
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
            options.UseNpgsql(
                _testConnectionString));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDatabaseContext>();

        services.AddSingleton(_configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPrivateKeyService, FakeKeyService>();
        services.AddScoped<ICookieService, FakeCookieService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBaseDatabaseRepository, BaseDatabaseRepository>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>();
        services.AddScoped<IEmailSender, FakeEmailSender>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddScoped<UserManager<ApplicationUser>>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFriendConnectionRepository, FriendConnectionRepository>();
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDatabaseContext>()
            .AddDefaultTokenProviders();
        

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _friendConnectionService =  _provider.GetRequiredService<IFriendConnectionService>();
        _userService = _provider.GetRequiredService<IApplicationUserService>();
        _userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        PopulateDbAndAddRoles.AddRolesAndAdminSync(_provider, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(_provider, testUserNumber, _context);
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
    public async Task GetUserCredentialsAsync_HandlesValidRequest_AndPreventEdgeCases()
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
    public async Task GetUserWithSentRequestsAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.GetUserWithFriendRequestsAsync(userIdForSuccess, u => u.SentFriendRequests) as SuccessWithDto<ApplicationUser>;
        Assert.That(0, Is.EqualTo(successResult.Data.SentFriendRequests.Count));
        
        await _friendConnectionService.SendFriendRequestAsync(userIdForSuccess, "TestUsername2");
        // After a friend request sent, the value will be +1
        var successResultAfterFriendSend = await _userService.GetUserWithFriendRequestsAsync(userIdForSuccess, u => u.SentFriendRequests) as SuccessWithDto<ApplicationUser>;
        Assert.That(1, Is.EqualTo(successResultAfterFriendSend.Data.SentFriendRequests.Count));
        
        // Failure, not existing user
        var userIdForFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureResult = await _userService.GetUserWithFriendRequestsAsync(userIdForFailure, u => u.SentFriendRequests);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }
    
    [Fact]
    public async Task GetUserWithReceivedRequestsAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.GetUserWithFriendRequestsAsync(userIdForSuccess, u => u.ReceivedFriendRequests) as SuccessWithDto<ApplicationUser>;
        Assert.That(0, Is.EqualTo(successResult.Data.ReceivedFriendRequests.Count));
        
        await _friendConnectionService.SendFriendRequestAsync(new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"), "TestUsername1");
        // After a friend request sent, the value will be +1
        var successResultAfterFriendSend = await _userService.GetUserWithFriendRequestsAsync(userIdForSuccess, u => u.ReceivedFriendRequests) as SuccessWithDto<ApplicationUser>;
        Assert.That(1, Is.EqualTo(successResultAfterFriendSend.Data.ReceivedFriendRequests.Count));
        
        // Failure, not existing user
        var userIdForFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureResult = await _userService.GetUserWithFriendRequestsAsync(userIdForFailure, u => u.ReceivedFriendRequests);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }

    [Fact]
    public async Task GetUserPrivateKey_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var roomIdForSuccess = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.GetUserPrivatekeyForRoomAsync(userIdForSuccess, roomIdForSuccess);
        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserPrivateKeyDto>>());
        
        //Failure test
        var userIdForFailure = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureResult = await _userService.GetUserPrivatekeyForRoomAsync(userIdForFailure, roomIdForSuccess);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User with the desired key not found.")));
    }

    [Fact]
    public async Task GetRoommatePublicKey_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var successResult = await _userService.GetRoommatePublicKey("TestUsername1");
        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserPublicKeyDto>>());
    }

    [Fact]
    public async Task ExamineIfUserHaveKeyForRoom_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var roomIdForSuccess = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var successResult = await _userService.ExamineIfUserHaveSymmetricKeyForRoom("TestUsername1", roomIdForSuccess);
        Assert.That(successResult, Is.InstanceOf<Success>());
        
        // Failure test
        var failureResult = await _userService.ExamineIfUserHaveSymmetricKeyForRoom("TestUsername2", roomIdForSuccess);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("The user don't have the key.")));
    }
    
    [Fact]
    public async Task SendForgotPasswordEmail_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var successResult = await _userService.SendForgotPasswordEmailAsync("test1@hotmail.com");
        Assert.That(successResult, Is.InstanceOf<SuccessWithMessage>());
        
        // Failure test
        var failureResult = await _userService.SendForgotPasswordEmailAsync($"test{testUserNumber + 1}@hotmail.com");
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }

    [Fact]
    public async Task SetNewPasswordAfterResetEmailAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "TestUsername1");
        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser!);

        var request = new PasswordResetRequest("test1@hotmail.com", "TestPassword123!!!");

        EmailSenderCodeGenerator.StorePasswordResetCode("test1@hotmail.com", token);

        var successResult = await _userService.SetNewPasswordAfterResetEmailAsync(token, request);
        Assert.That(successResult, Is.InstanceOf<Success>());
        
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "TestUsername1");
        var isPasswordCorrect = await _userManager.CheckPasswordAsync(updatedUser!, "TestPassword123!!!");
        Assert.That(isPasswordCorrect, Is.True);

        // Failure test with invalid token
        var failureResult = await _userService.SetNewPasswordAfterResetEmailAsync("invalidToken", request);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("Invalid or expired reset code.")));
    }
    
    [Fact]
    public async Task ChangeUserEmailAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var request = new ChangeEmailRequest("test1@hotmail.com", "newtest1@gmail.com");
        var successResult = await _userService.ChangeUserEmailAsync(request, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserEmailDto>>());
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "TestUsername1");
        Assert.That("newtest1@gmail.com", Is.EqualTo(updatedUser.Email));
        
        // Failure with not existing/ wrong email address
        var failureRequestNotExisting = new ChangeEmailRequest("notexisting@hotmail.com", "newtest1@gmail.com");
        var failureRequestNotExistingResult = await _userService.ChangeUserEmailAsync(failureRequestNotExisting, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915"));
        Assert.That(failureRequestNotExistingResult, Is.EqualTo(new FailureWithMessage("User not found.")));
        
        // Failure with wrong email address
        var failureRequestWrongEmail = new ChangeEmailRequest("test@hotmail.com", "newtest1@gmail.com");
        var failureRequestWrongEmailResult = await _userService.ChangeUserEmailAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That(failureRequestWrongEmailResult, Is.EqualTo(new FailureWithMessage("E-mail address not valid.")));
        
        // Failure, the email is already in use
        var failureExistingEmail = new ChangeEmailRequest("newtest1@gmail.com", "test2@hotmail.com");
        var failureExistingEmailResult = await _userService.ChangeUserEmailAsync(failureExistingEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That(failureExistingEmailResult, Is.EqualTo(new FailureWithMessage("This email is already in use.")));
    }
    
    [Fact]
    public async Task ChangeUserPasswordAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        var request = new ChangePasswordRequest("testUserPassword123###", "changedUserPassword123###");
        var successResult = await _userService.ChangeUserPasswordAsync(request, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserNameEmailDto>>());
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "TestUsername1");
        var isPasswordCorrect = await _userManager.CheckPasswordAsync(updatedUser!, "changedUserPassword123###");
        Assert.That(isPasswordCorrect, Is.True);
        
        // Failure with not existing/ wrong email address
        var failureRequestNotExisting = new ChangePasswordRequest("notexisting@hotmail.com", "newtest1@gmail.com");
        var failureRequestNotExistingResult = await _userService.ChangeUserPasswordAsync(failureRequestNotExisting, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915"));
        Assert.That(failureRequestNotExistingResult, Is.EqualTo(new FailureWithMessage("User not found.")));
        
        // After 5 failure attempts, the account will be locked for 1 day
        var failureRequestWrongEmail = new ChangePasswordRequest("testUserPassword123##", "changedUserPassword123###");
        await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var result = await _userService.ChangeUserPasswordAsync(failureRequestWrongEmail, Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        Assert.That((result as FailureWithMessage).Message.Contains("Account is locked."));
    }

    [Fact]
    public async Task DeleteUserAsync_HandlesValidRequest_AndPreventEdgeCases()
    {
        // Success test
        const string userPassword = "testUserPassword123###";
        var userId = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var result =  await _userService.DeleteUserAsync(userId, userPassword);
        Assert.That(result, Is.InstanceOf<SuccessWithDto<UserNameEmailDto>>());
        var nullResult = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "TestUsername1");
        Assert.That(nullResult, Is.Null);
        
        // Failure, user not existing
        var failureResult =  await _userService.DeleteUserAsync(userId, userPassword);
        Assert.That(failureResult, Is.EqualTo(new FailureWithMessage("User not found.")));
        
        //Failure with wrong password
        var failureWrongPasswordResult =  await _userService.DeleteUserAsync(new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"), "testUserPassword123##");
        Assert.That(failureWrongPasswordResult, Is.EqualTo(new FailureWithMessage("Invalid credentials.")));
    }
}