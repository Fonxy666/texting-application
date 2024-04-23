using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Message;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class MessageControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");

    public MessageControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Get_Message_ReturnsBadRequest_With_Invalid_RoomId()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string roomId = "123";

        var getUserResponse = await _client.GetAsync($"/Message/getMessages/{roomId}");
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
        
        Assert.Contains($"There is no room with this id: {roomId}", responseContent);
    }
    
    [Fact]
    public async Task Get_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string roomId = "233bb449-030d-46d2-bcb5-6742bc3eb3a8";

        var getUserResponse = await _client.GetAsync($"/Message/getMessages/{roomId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Send_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageRequest = new MessageRequest("233bb449-030d-46d2-bcb5-6742bc3eb3a8", _testUser.UserName, "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        roomRegistrationResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Send_Message_ReturnBadRequest_With_Invalid_ModelState()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageRequest = "";
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.BadRequest, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task Send_Message_ReturnBadRequest_With_Not_Valid_RoomId()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageRequest = new MessageRequest("123", _testUser.UserName, "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.InternalServerError, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task Edit_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageChangeRequest = new EditMessageRequest("f3190268-ee9a-4348-b1f6-4b146c09901f", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/Message/EditMessage", messageChange);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Edit_Message_ReturnBadRequest_With_Invalid_ModelState()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("/Message/EditMessage", messageChange);
        
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task Delete_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string messageDeleteRequestId = "a57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"/Message/DeleteMessage?id={messageDeleteRequestId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
}