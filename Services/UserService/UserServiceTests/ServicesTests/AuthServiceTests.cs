using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.MediaService;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.User;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace UserServiceTests.ServicesTests;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IAuthService _authService;
    private readonly MainDatabaseContext _context;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        var services = new ServiceCollection();
        
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();

        services.AddDbContext<MainDatabaseContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5434;Username=postgres;Password=testPassword123@;Database=test_user_db;SSL Mode=Disable;"));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDatabaseContext>();

        services.AddSingleton(_configuration);
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IPrivateKeyService, FakeKeyService>();
        services.AddScoped<ICookieService, FakeCookieService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IImageService, FakeImageService>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _authService = _provider.GetRequiredService<IAuthService>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        var app = _provider.GetRequiredService<IApplicationBuilder>();
        PopulateDbAndAddRoles.AddRolesAndAdminSync(app, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(app, 1, _context);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RegisterAsync_HandlesValidRegistration_AndPreventsDuplicateEmailUsernamePhone()
    {
        // Success test
        var request = new RegistrationRequest("test1@example.com", "testUserName", "Password123!", "IMAGE",
            "06201234567", "publicKeyHere", "encryptedPrivateKeyHere", "ivDataHere");

        var imagePath = "fake/image/path.jpg";

        var result = await _authService.RegisterAsync(request);

        Assert.That(result, Is.InstanceOf<Success>());

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "testUserName");
        Assert.That(user, Is.Not.Null);
        Assert.That(user.Email, Is.EqualTo(request.Email));
        
        // Failure with already in use email
        var existingEmailRequest = new RegistrationRequest("test1@example.com", "newTestUserName", "Password123!", "IMAGE",
            "06201234568", "publicKeyHere", "encryptedPrivateKeyHere", "ivDataHere");

        var existingEmailResult = await _authService.RegisterAsync(existingEmailRequest);

        Assert.That(existingEmailResult, Is.EqualTo(new FailureWithMessage("Email is already taken")));
        
        // Failure with already in use username
        var existingUsernameRequest = new RegistrationRequest("test2@example.com", "testUserName", "Password123!", "IMAGE",
            "06201234568", "publicKeyHere", "encryptedPrivateKeyHere", "ivDataHere");

        var existingUsernameResult = await _authService.RegisterAsync(existingUsernameRequest);

        Assert.That(existingUsernameResult, Is.EqualTo(new FailureWithMessage("Username is already taken")));
        
        // Failure with already in use phone number
        var existingPhoneNumberRequest = new RegistrationRequest("test2@example.com", "newTestUserName", "Password123!", "IMAGE",
            "06201234567", "publicKeyHere", "encryptedPrivateKeyHere", "ivDataHere");

        var existingPhoneNumberResult = await _authService.RegisterAsync(existingPhoneNumberRequest);
        
        Assert.That(existingPhoneNumberResult, Is.EqualTo(new FailureWithMessage("Phone number is already taken")));
    }

    [Fact]
    public async Task LoginAsync_HandlesValidLogin_AndPreventsLoginWithInvalidData()
    {
        var token = EmailSenderCodeGenerator.GenerateShortToken("test1@hotmail.com", EmailType.Login);
        // Success test
        var successRequest = new LoginAuth( "TestUsername1", true, token);

        var successResult = await _authService.LoginAsync(successRequest);

        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<KeysDto>>());
        
        // Failure with wrong token
        var wrongToken = EmailSenderCodeGenerator.GenerateShortToken("bad@hotmail.com", EmailType.Login);
        var wrongWIthBBadTokenRequest = new LoginAuth( "TestUsername1", true, wrongToken);
        var failureWIthWrongTokenResult = await _authService.LoginAsync(wrongWIthBBadTokenRequest);

        Assert.That(failureWIthWrongTokenResult, Is.EqualTo(new FailureWithMessage("The provided login code is not correct.")));
    }
    
    [Fact]
    public async Task LogOut_HandlesValidLogOut_HandlesErrorIfUserNotFound()
    {
        // Success test
        var testUserId = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");

        var successResult = await _authService.LogOutAsync(testUserId);

        Assert.That(successResult, Is.InstanceOf<Success>());
        
        // Failure with wrong token
        var testUserWrongId = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");
        var failureWIthWrongTokenResult = await _authService.LogOutAsync(testUserWrongId);

        Assert.That(failureWIthWrongTokenResult, Is.EqualTo(new FailureWithMessage("User not found")));
    }
}
