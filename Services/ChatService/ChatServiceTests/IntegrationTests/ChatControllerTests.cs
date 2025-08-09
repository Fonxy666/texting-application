using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
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
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace ChatServiceTests.IntegrationTests;

public class ChatControllerTests : IClassFixture<WebApplicationFactory<Startup>>, IAsyncLifetime
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
    private readonly Guid _testUserIdForBadRequests = Guid.Parse("3f3b1278-5c3e-4d51-842f-14d2a6581e2e");
    
    public ChatControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        
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
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(_configuration);
            })
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserGrpcService>();
                services.AddScoped<IUserGrpcService>(_ => new FakeUserGrpcService());
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
        _provider = _testServer.Host.Services;
        _context = _provider.GetRequiredService<ChatContext>();
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION during InitializeAsync: " + ex);
            throw;
        }
    }

    public Task DisposeAsync()
    {
        _testServer.Dispose(); 
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RegisterRoom_CreatesTheRoom_AndPreventEdgeCases()
    {
        var notExistingUserId = Guid.NewGuid();
        var notExistingUserJwt = FakeLogin.TestJwtSecurityToken(notExistingUserId.ToString(), _configuration);
        var notExistingUserJwtString = new JwtSecurityTokenHandler().WriteToken(notExistingUserJwt);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notExistingUserJwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={notExistingUserId}");
        
        var notExistingUserRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var notExistingUserJsonRequest = JsonConvert.SerializeObject(notExistingUserRequest);
        var notExistingUserContent = new StringContent(notExistingUserJsonRequest, Encoding.UTF8, "application/json");

        var notExistingUserResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", notExistingUserContent);
        Assert.Equal(HttpStatusCode.NotFound, notExistingUserResponse.StatusCode);
        
        _client.DefaultRequestHeaders.Clear();
        
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");

        var request = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Chat/RegisterRoom", content);
        response.EnsureSuccessStatusCode();
        
        var failureResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", content);
        Assert.Equal(HttpStatusCode.BadRequest, failureResponse.StatusCode);
    }
    
    [Fact]
    public async Task ExamineCreator_ResponseTheValidState_AndPreventEdgeCases()
    {
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var request = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", content);

        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        var response = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={roomId}");
        
        Assert.Equal("true", response.Content.ReadAsStringAsync().Result);
        response.EnsureSuccessStatusCode();
        
        var wrongResponse = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={Guid.NewGuid()}");
        Assert.Equal("false", wrongResponse.Content.ReadAsStringAsync().Result);
        wrongResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteRoom_ResponseTheValidState_AndPreventEdgeCases()
    {
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var request = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", content);
        
        _client.DefaultRequestHeaders.Clear();
        
        var newJwt = FakeLogin.TestJwtSecurityToken(_testUserIdForBadRequests.ToString(), _configuration);
        var newUserJwtString = new JwtSecurityTokenHandler().WriteToken(newJwt);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserJwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserIdForBadRequests}");
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        
        var forbidResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?roomId={roomId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidResponse.StatusCode);
        
        _client.DefaultRequestHeaders.Clear();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var response = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?roomId={roomId}");
        response.EnsureSuccessStatusCode();
        
        var notFoundResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?roomId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_ResponseTheValidState_AndPreventEdgeCases()
    {
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        _client.DefaultRequestHeaders.Clear();
        
        var newJwt = FakeLogin.TestJwtSecurityToken(_testUserIdForBadRequests.ToString(), _configuration);
        var newUserJwtString = new JwtSecurityTokenHandler().WriteToken(newJwt);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserJwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserIdForBadRequests}");
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        var request = new ChangeRoomPassword(roomId, "testPassword", "newTestPassword");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        var forbidResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", content);
        
        Assert.Equal(HttpStatusCode.Forbidden, forbidResponse.StatusCode);
        
        _client.DefaultRequestHeaders.Clear();
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var response = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", content);
        response.EnsureSuccessStatusCode();
        
        var badRequest = new ChangeRoomPassword(Guid.NewGuid(), "newTestPassword", "newTestPassword123");
        var badJsonRequest = JsonConvert.SerializeObject(badRequest);
        var badContent = new StringContent(badJsonRequest, Encoding.UTF8, "application/json");
        
        var notFoundResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", badContent);
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
    }
    
    [Fact]
    public async Task LoginRoom_ResponseTheValidState_AndPreventEdgeCases()
    {
        var jwt = FakeLogin.TestJwtSecurityToken(_testUserId.ToString(), _configuration);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        _client.DefaultRequestHeaders.Add("Cookie", $"UserId={_testUserId}");
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        var roomName = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomName;
        var request = new JoinRoomRequest(roomName, "testPassword");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("api/v1/Chat/JoinRoom", content);
        response.EnsureSuccessStatusCode();
        
        var badRequest = new JoinRoomRequest("wrongRoomName", "testPassword");
        var badJsonRequest = JsonConvert.SerializeObject(badRequest);
        var badContent = new StringContent(badJsonRequest, Encoding.UTF8, "application/json");
        
        var notFoundResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", badContent);
        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        
    }
}