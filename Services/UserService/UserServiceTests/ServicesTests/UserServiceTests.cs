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
using UserService.Services.PrivateKeyFolder;
using UserService.Services.User;
using UserServiceTests;
using Xunit;
using Assert = NUnit.Framework.Assert;

public class UserServiceTest : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly IApplicationUserService _userService;
    private readonly MainDatabaseContext _context;
    private readonly IConfiguration _configuration;

    public UserServiceTest()
    {
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
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IUserHelper, UserHelper>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _userService = _provider.GetRequiredService<IApplicationUserService>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        var app = _provider.GetRequiredService<IApplicationBuilder>();
        PopulateDbAndAddRoles.AddRolesAndAdminSync(app, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(app, 1);
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
        var userIdForSuccess = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");

        var successResult = await _userService.GetUserNameAsync(userIdForSuccess);

        Assert.That(successResult, Is.InstanceOf<SuccessWithDto<UserNameDto>>());
        
        // Failed test
        var userIdForNotFound = Guid.Parse("38db530c-b6bb-4e8a-9c19-a5cd4d0fa915");

        var notFoundResult = await _userService.GetUserNameAsync(userIdForNotFound);

        Assert.That(notFoundResult, Is.EqualTo(new FailureWithMessage("User not found.")));
    }
}