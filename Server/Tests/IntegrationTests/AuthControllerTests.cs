using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Server;
using Server.Database;
using Server.Requests;
using Server.Responses;
using Server.Services.Authentication;
using Xunit;
using Assert = Xunit.Assert;
using System.Text.Json;
using Server.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Startup>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<UsersContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<UsersContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<UsersContext>()
                .AddDefaultTokenProviders();
        });
    }
}

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;
    private readonly Mock<IAuthService> _authServiceMock;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
        _authServiceMock = new Mock<IAuthService>();
        ConfigureAuthService(factory);
    }
    
    private void ConfigureAuthService(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var testUser = new ApplicationUser("url") { UserName = "adminGod", Email = "adminGod@adminGod.com" };
        userManager.CreateAsync(testUser, "asdASDasd123666").GetAwaiter().GetResult();
        
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AuthResult(true, "adminGod@adminGod.com", "adminGod",
                CreateToken(factory, testUser, "Admin")));
    }
    
    private string CreateToken(CustomWebApplicationFactory factory, ApplicationUser user, string role)
    {
        var tokenService = new TokenService(factory.Services.GetRequiredService<IConfiguration>());
        return tokenService.CreateToken(user, role, false);
    }

    private async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    
    [Fact]
    public async Task Authenticate_Returns_OkResult_With_Valid_Credentials()
    {
        var request = new AuthRequest("Admin", "asdASDasd123666");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Login", content);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
    
    [Fact]
    public async Task Authenticate_Returns_BadRequest_With_Not_Valid_Credentials()
    {
        var request = new AuthRequest("Admin", "notValidPassword123");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Login", content);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }

    [Fact]
    public async Task Register_Returns_OkResult_With_Valid_Credentials()
    {
        var request = new RegistrationRequest("user@test.com", "test-user", "PasswordToTestUser", "06201234567",
            "");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Register", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
    
    [Fact]
    public async Task Register_Returns_BadRequest_With_Not_Valid_Credentials()
    {
        var request = new RegistrationRequest("", "test-user", "PasswordToTestUser", "06201234567",
            "");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Register", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
}