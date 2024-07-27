using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Message;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class MessageControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly AuthRequest _testUser3 = new ("TestUsername3", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public MessageControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testConfiguration.json")
            .Build();
        
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(configuration);
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
    }
    
    [Fact]
    public async Task GetMessage_WithInvalidRoomId_ReturnsBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string roomId = "a57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.GetAsync($"api/v1/Message/getMessages/{roomId}");
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
        
        Assert.Contains($"There is no room with this id: {roomId}", responseContent);
    }
    
    [Fact]
    public async Task SendMessage_GetMessage_WithValidRequest_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageRequest = new MessageRequest("901d40c6-c95d-47ed-a21a-88cda341d0a9", "test", false, "testIv", "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        sendMessageResponse.EnsureSuccessStatusCode();

        var getUserResponse = await _client.GetAsync($"api/v1/Message/getMessages/{messageRequest.RoomId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendMessage_ToNotExistingRoom_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageRequest = new MessageRequest(Guid.NewGuid().ToString(), "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        Assert.Equal(HttpStatusCode.NotFound, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendMessage_WithInvalidModelState_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageRequest = "";
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.BadRequest, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithValidCredentials_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageChangeRequest = new EditMessageRequest(new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c"), "TestChange", "testIv");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getMessageResponse = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        getMessageResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task EditMessage_WithInvalidUser_ReturnBadRequest()
    {
        var cookies1 = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies1);
        
        var messageRequest = new MessageRequest("901d40c6-c95d-47ed-a21a-88cda341d0a9", "test", false, "testIv", "a57f0d67-8670-4789-a580-4b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Message/SendMessage", contentSend);

        _client.DefaultRequestHeaders.Remove("Cookie");
        var cookies2 = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies2);
        
        var messageChangeRequest = new EditMessageRequest(new Guid("a57f0d67-8670-4789-a580-4b4a3bd3bf9c"), "TestChange", "testIv");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getMessageResponse = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        Assert.Equal(HttpStatusCode.BadRequest, getMessageResponse.StatusCode);
        
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", cookies1);
        
        const string messageDeleteRequestId = "a57f0d67-8670-4789-a580-4b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageDeleteRequestId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessage_WithInvalidModelState_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageResponse = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        Assert.Equal(HttpStatusCode.BadRequest, editMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithNotExistingMessageId_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageChangeRequest = new EditMessageRequest(Guid.NewGuid(), "TestChange", "testIv");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        
        Assert.Equal(HttpStatusCode.NotFound, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task EditMessageSeen_WithValidRequest_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageChangeRequest = new EditMessageSeenRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        editMessageRequest.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidMessageId_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var messageChangeRequest = new EditMessageSeenRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        editMessageRequest.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidModel_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteMessage_WithValidMessageId_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string messageDeleteRequestId = "a57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageDeleteRequestId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteMessage_WithNotTheSender_ReturnBadRequest()
    {
        var cookies1 = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies1);
        const string messageId = "c57f0d67-8670-4789-a580-3b4a3bd3bf9c";
        
        var messageRequest = new MessageRequest("901d40c6-c95d-47ed-a21a-88cda341d0a9", "test", false, "iv", messageId);
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        await _client.PostAsync("api/v1/Message/SendMessage", contentSend);

        _client.DefaultRequestHeaders.Remove("Cookies");
        var cookies2 = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies2);

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageId}");
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
        
        _client.DefaultRequestHeaders.Remove("Cookies");
        _client.DefaultRequestHeaders.Add("Cookie", cookies1);
        
        await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageId}");
    }
    
    [Fact]
    public async Task DeleteMessage_WithNotExistingMessageId_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string messageDeleteRequestId = "b57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageDeleteRequestId}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
}