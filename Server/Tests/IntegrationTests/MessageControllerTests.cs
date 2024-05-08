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
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;


    public MessageControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();

        configuration["ConnectionString"] = "Server=localhost,1434;Database=textinger_test_database;User Id=sa;Password=yourStrong(!)Password;MultipleActiveResultSets=true;TrustServerCertificate=True";
        configuration["IssueAudience"] = "api With Authentication for Tests correctly implemented";
        configuration["IssueSign"] = "V3ryStr0ngP@ssw0rdW1thM0reTh@n256B1ts4Th3T3sts";
        configuration["AdminEmail"] = "AdminEmail";
        configuration["AdminUserName"] = "AdminUserName";
        configuration["AdminPassword"] = "AdminPassword";
        configuration["DeveloperEmail"] = "DeveloperEmail";
        configuration["DeveloperPassword"] = "DeveloperPassword";
        configuration["GoogleClientId"] = "GoogleClientId";
        configuration["GoogleClientSecret"] = "GoogleClientSecret";
        configuration["FacebookClientId"] = "FacebookClientId";
        configuration["FacebookClientSecret"] = "FacebookClientSecret";
        configuration["FrontendUrlAndPort"] = "http://localhost:4200";
        
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(configuration);
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
        var cookies = TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }
    
    [Fact]
    public async Task GetMessage_WithInvalidRoomId_ReturnsBadRequest()
    {
        const string roomId = "123";

        var getUserResponse = await _client.GetAsync($"api/v1/Message/getMessages/{roomId}");
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
        
        Assert.Contains($"There is no room with this id: {roomId}", responseContent);
    }
    
    [Fact]
    public async Task SendMessage_GetMessage_WithValidRequest_ReturnSuccessStatusCode()
    {
        var messageRequest = new MessageRequest("ea5c5adb-9807-4ad1-b6da-7650d821827a", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        roomRegistrationResponse.EnsureSuccessStatusCode();

        var getUserResponse = await _client.GetAsync($"api/v1/Message/getMessages/{messageRequest.RoomId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendMessage_WithInvalidModelState_ReturnBadRequest()
    {
        var messageRequest = "";
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.BadRequest, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendMessage_WithNotValidRoomId_ReturnBadRequest()
    {
        var messageRequest = new MessageRequest("123", _testUser.UserName, "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.NotFound, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendMessage_WithNotValidUserId_ReturnBadRequest()
    {
        var messageRequest = new MessageRequest("ea5c5adb-9807-4ad1-b6da-7650d821827a", _testUser.UserName, "test", false, "123");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("api/v1/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.NotFound, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithValidCredentials_ReturnSuccessStatusCode()
    {
        var messageChangeRequest = new EditMessageRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessage_WithNotExistingId_ReturnNotFound()
    {
        var messageChangeRequest = new EditMessageRequest("1", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithInvalidModelState_ReturnBadRequest()
    {
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessage", messageChange);
        
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task EditMessageSeen_WithValidRequest_ReturnSuccessStatusCode()
    {
        var messageChangeRequest = new EditMessageSeenRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "18c5eb4f-b614-45d0-9ee8-ad7f17e88dd9");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        editMessageRequest.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidMessageId_ReturnNotFound()
    {
        var messageChangeRequest = new EditMessageSeenRequest("1", "18c5eb4f-b614-45d0-9ee8-ad7f17e88dd9");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        Assert.Equal(HttpStatusCode.NotFound, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidModel_ReturnBadRequest()
    {
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("api/v1/Message/EditMessageSeen", messageChange);
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteMessage_WithValidMessageId_ReturnSuccessStatusCode()
    {
        const string messageDeleteRequestId = "a57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageDeleteRequestId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteMessage_WithInvalidModel_ReturnBadRequest()
    {
        var messageDeleteRequestId = new EditMessageSeenRequest("", "");

        var getUserResponse = await _client.DeleteAsync($"api/v1/Message/DeleteMessage?id={messageDeleteRequestId}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
}