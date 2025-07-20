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
using Assert = Xunit.Assert;

namespace UserServiceTests.IntegrationTests;

[Collection("Sequential")]
public class CookiesControllerTests : IClassFixture<WebApplicationFactory<Startup>>, IAsyncLifetime
{
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _vaultClient;
    private readonly MainDatabaseContext _context;
    private readonly string _testConnectionString;
    private readonly string _hashiCorpTestToken;
    private readonly string _hashiCorpTestAddress;
    
    public CookiesControllerTests()
    {
        var baseConfig  = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();
        
        _testConnectionString = baseConfig["UserTestDbConnectionString"]!;
        _hashiCorpTestAddress = baseConfig["HashiCorpTestAddress"]!;
        _hashiCorpTestToken = baseConfig["HashiCorpTestToken"]!;
        
        _configuration = new ConfigurationBuilder()
            .AddConfiguration(baseConfig)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", _testConnectionString }
            }!)
            .Build();
        
        _vaultClient = new HttpClient
        {
            BaseAddress = new Uri(_hashiCorpTestAddress)
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
                services.RemoveAll<IPrivateKeyService>();
                services.AddScoped<IPrivateKeyService>(_ =>
                    new PrivateKeyService(_vaultClient!, _hashiCorpTestToken, _hashiCorpTestAddress));
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