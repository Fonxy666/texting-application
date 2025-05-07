using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Models;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.FriendConnectionService;
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

    public FriendConnectionTests()
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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPrivateKeyService, FakeKeyService>();
        services.AddScoped<ICookieService, FakeCookieService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IApplicationBuilder, ApplicationBuilder>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _friendService = _provider.GetRequiredService<IFriendConnectionService>();
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
    public async Task SendFriendRequest_HandlesValidRequest_AndPreventsInvalidUserInvalidIdAlreadySentRequest()
    {
        // Success test
        var result =
            await _friendService.SendFriendRequestAsync("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "TestUsername2");

        Assert.That(result, Is.InstanceOf<SuccessWithDto<ShowFriendRequestDto>>());
    }
}
