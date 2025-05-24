using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using UserService;
using UserService.Database;
using UserService.Models.Requests;
using UserService.Services.PrivateKeyFolder;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace UserServiceTests.IntegrationTests;

[Collection("Sequential")]
public class CookiesControllerTests : IClassFixture<WebApplicationFactory<Startup>>, IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _vaultClient;
    private readonly MainDatabaseContext _context;
    
    public CookiesControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();
        
        _vaultClient = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:8201")
        };
        
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(_configuration);
            })
            .ConfigureTestServices(services =>
            {
                var token = "root"; // for test only
                var address = "http://127.0.0.1:8201";

                services.RemoveAll<IPrivateKeyService>();
                services.AddScoped<IPrivateKeyService>(_ =>
                    new PrivateKeyService(_vaultClient!, token, address));
            });
        
        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
        _provider = _testServer.Host.Services;
        _context = _provider.GetRequiredService<MainDatabaseContext>();
    }
    
    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        PopulateDbAndAddRoles.AddRolesAndAdminSync(_provider, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(_provider, 5, _context);
        
        var cookies = TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }

    public Task DisposeAsync()
    {
        _testServer.Dispose(); 
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task ChangeCookies_WithValidRequest_ReturnOkStatusCode()
    {
        var firstRequestData = new { request = "Animation" };
        var firstJsonRequest = JsonConvert.SerializeObject(firstRequestData);
        var firstContent = new StringContent(firstJsonRequest, Encoding.UTF8, "application/json");
        
        var firstResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=Animation", firstContent);
    
        var secondRequestData = new { request = "Anonymous" };
        var secondJsonRequest = JsonConvert.SerializeObject(secondRequestData);
        var secondContent = new StringContent(secondJsonRequest, Encoding.UTF8, "application/json");
        
        var secondResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=Anonymous", secondContent);
        
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeCookies_WithInvalidParams_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject("asd");
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var roomRegistrationResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=asd", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}