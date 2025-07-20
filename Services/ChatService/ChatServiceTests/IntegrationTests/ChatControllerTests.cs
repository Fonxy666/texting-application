using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ChatService;
using ChatService.Database;
using ChatService.Model.Requests;
using ChatService.Services.Chat.GrpcService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace ChatServiceTests.IntegrationTests;

public class ChatControllerTests : IClassFixture<WebApplicationFactory<TestStartup>>, IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly TestServer _testServer;
    private readonly ChatContext _context;
    private readonly IServiceProvider _provider;
    private readonly IUserGrpcService _userGrpcService;
    private readonly IConfiguration _configuration;
    private readonly string _testConnectionString;
    private readonly Guid _testUserId = Guid.Parse("2f1b9e96-8c0b-4a4b-8fd3-9b4c0a447e31");
    
    public ChatControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        var baseConfig  = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("chat-service-test-config.json")
            .AddEnvironmentVariables()
            .Build();
        
        _testConnectionString = baseConfig["ChatTestDbConnectionString"]!;
        
        _configuration = new ConfigurationBuilder()
            .AddConfiguration(baseConfig)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", _testConnectionString }
            }!)
            .Build();

        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<TestStartup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(_configuration);
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
        _provider = _testServer.Host.Services;
        _context = _provider.GetRequiredService<ChatContext>();
        _userGrpcService =  _provider.GetRequiredService<IUserGrpcService>();
    }
    
    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _testServer.Dispose(); 
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_WithInvalidUser_ReturnBadRequestStatusCode()
    {
        // Arrange
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");

        var request = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("api/v1/Chat/RegisterRoom", content);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        _testOutputHelper.WriteLine(body);
        response.EnsureSuccessStatusCode();
    }
}