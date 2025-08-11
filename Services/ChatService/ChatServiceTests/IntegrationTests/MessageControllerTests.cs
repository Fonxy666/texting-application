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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace ChatServiceTests.IntegrationTests;

public class MessageControllerTests : IClassFixture<WebApplicationFactory<Startup>>, IAsyncLifetime
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
    
    public MessageControllerTests(ITestOutputHelper testOutputHelper)
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
    public async Task SendMessage_SendMessage_AndPreventEdgeCases()
    {
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        
        var request = new MessageRequest(roomId, "testMessage", false, "newTestPassword", null);
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("api/v1/Message/SendMessage", content);
        
        response.EnsureSuccessStatusCode();
        
        var roomNotExistMessageRequest = new MessageRequest(Guid.NewGuid(), "testMessage", false, "newTestPassword", null);
        var roomNotExistJsonRequest = JsonConvert.SerializeObject(roomNotExistMessageRequest);
        var roomNotExistContent = new StringContent(roomNotExistJsonRequest, Encoding.UTF8, "application/json");
        
        var roomNotExistResponse = await _client.PostAsync("api/v1/Message/SendMessage", roomNotExistContent);
        Assert.Equal(HttpStatusCode.NotFound, roomNotExistResponse.StatusCode);
        
        FakeLogin.FakeLoginToClient(_client, Guid.NewGuid(), _configuration);
        
        var userNotExistResponse = await _client.PostAsync("api/v1/Message/SendMessage", content);
        Assert.Equal(HttpStatusCode.NotFound, userNotExistResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetMessages_GetAllMessages_AndPreventEdgeCases()
    {
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        
        var response = await _client.GetAsync($"api/v1/Message/GetMessages/{roomId}/{1}");
        
        response.EnsureSuccessStatusCode();
        
        var roomNotExistResponse = await _client.GetAsync($"api/v1/Message/GetMessages/{Guid.NewGuid()}/{1}");
        Assert.Equal(HttpStatusCode.NotFound, roomNotExistResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_EditTheMessage_AndPreventEdgeCases()
    {
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        
        var sendMessageRequest = new MessageRequest(roomId, "testMessage", false, "newTestPassword", null);
        var sendMessageJsonRequest = JsonConvert.SerializeObject(sendMessageRequest);
        var sendMessageContent = new StringContent(sendMessageJsonRequest, Encoding.UTF8, "application/json");
        
        await _client.PostAsync("api/v1/Message/SendMessage", sendMessageContent);
        
        var message = _context.Messages.FirstOrDefault(m => m.Text == "testMessage");
        
        var modifyMessageRequest = new EditMessageRequest(message.MessageId, "testMessage", message.Iv);
        var modifyMessageJsonRequest = JsonConvert.SerializeObject(modifyMessageRequest);
        var modifyMessageContent = new StringContent(modifyMessageJsonRequest, Encoding.UTF8, "application/json");
        
        var response = await _client.PatchAsync($"api/v1/Message/EditMessage", modifyMessageContent);
        
        response.EnsureSuccessStatusCode();
        
        var notExistingMessageRequest = new EditMessageRequest(Guid.NewGuid(), "testMessage", message.Iv);
        var notExistingMessageJsonRequest = JsonConvert.SerializeObject(notExistingMessageRequest);
        var notExistingMessageContent = new StringContent(notExistingMessageJsonRequest, Encoding.UTF8, "application/json");
        
        var notExistingMessageResponse = await _client.PatchAsync($"api/v1/Message/EditMessage", notExistingMessageContent);
        Assert.Equal(HttpStatusCode.NotFound, notExistingMessageResponse.StatusCode);
        
        FakeLogin.FakeLoginToClient(_client, _testUserIdForBadRequests, _configuration);
        
        var forbidMessageRequest = new EditMessageRequest(message.MessageId, "testMessage", message.Iv);
        var forbidMessageJsonRequest = JsonConvert.SerializeObject(forbidMessageRequest);
        var forbidMessageContent = new StringContent(forbidMessageJsonRequest, Encoding.UTF8, "application/json");
        
        var forbidResponse = await _client.PatchAsync($"api/v1/Message/EditMessage", forbidMessageContent);
        Assert.Equal(HttpStatusCode.Forbidden, forbidResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteMessage_DeleteTheMessage_AndPreventEdgeCases()
    {
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);
        
        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);
        
        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;
        
        var sendMessageRequest = new MessageRequest(roomId, "testMessage", false, "newTestPassword", null);
        var sendMessageJsonRequest = JsonConvert.SerializeObject(sendMessageRequest);
        var sendMessageContent = new StringContent(sendMessageJsonRequest, Encoding.UTF8, "application/json");
        
        await _client.PostAsync("api/v1/Message/SendMessage", sendMessageContent);
        
        var messageId = _context.Messages.FirstOrDefault(m => m.Text == "testMessage").MessageId;
        
        FakeLogin.FakeLoginToClient(_client, Guid.NewGuid(), _configuration);
        
        var forbidResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?messageId={messageId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidResponse.StatusCode);
        
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);
        
        var response = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?messageId={messageId}");
        
        response.EnsureSuccessStatusCode();
        
        var notExistingMessageResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?messageId={messageId}");
        Assert.Equal(HttpStatusCode.NotFound, notExistingMessageResponse.StatusCode);
    }

    [Fact]
    public async Task MOdifyMessageSeen_ModifyTheSeenList_AndPreventEdgeCases()
    {
        FakeLogin.FakeLoginToClient(_client, _testUserId, _configuration);

        var roomRequest = new RoomRequest("testRoomName", "testPassword", "testEncryptedKey");
        var roomJsonRequest = JsonConvert.SerializeObject(roomRequest);
        var roomContent = new StringContent(roomJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Chat/RegisterRoom", roomContent);

        var roomId = _context.Rooms.FirstOrDefault(r => r.RoomName == "testRoomName").RoomId;

        var sendMessageRequest = new MessageRequest(roomId, "testMessage", false, "newTestPassword", null);
        var sendMessageJsonRequest = JsonConvert.SerializeObject(sendMessageRequest);
        var sendMessageContent = new StringContent(sendMessageJsonRequest, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Message/SendMessage", sendMessageContent);

        var message = _context.Messages.FirstOrDefault(m => m.Text == "testMessage");

        var modifyMessageRequest = new EditMessageSeenRequest(message.MessageId);
        var modifyMessageJsonRequest = JsonConvert.SerializeObject(modifyMessageRequest);
        var modifyMessageContent = new StringContent(modifyMessageJsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PatchAsync($"api/v1/Message/EditMessageSeen", modifyMessageContent);

        response.EnsureSuccessStatusCode();

        var notExistingMessageRequest = new EditMessageSeenRequest(Guid.NewGuid());
        var notExistingMessageJsonRequest = JsonConvert.SerializeObject(notExistingMessageRequest);
        var notExistingMessageContent = new StringContent(notExistingMessageJsonRequest, Encoding.UTF8, "application/json");

        var notExistingMessageResponse =
            await _client.PatchAsync($"api/v1/Message/EditMessageSeen", notExistingMessageContent);
        Assert.Equal(HttpStatusCode.NotFound, notExistingMessageResponse.StatusCode);
    }
}