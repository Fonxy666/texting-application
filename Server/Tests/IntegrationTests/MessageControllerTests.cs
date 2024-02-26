using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests.Auth;
using Server.Requests.Chat;
using Server.Requests.Message;
using Xunit;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class MessageControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###", false);
    private readonly RoomRequest _testRoom = new ("test", "test");

    public MessageControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Get_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string roomId = "bbdcc735-d897-45a7-b10d-62c57b52fcca";

        var getUserResponse = await _client.GetAsync($"/Message/getMessages/{roomId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Send_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageRequest = new MessageRequest("bbdcc735-d897-45a7-b10d-62c57b52fcca", _testUser.UserName, "test", "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        roomRegistrationResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Edit_Message_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var messageChangeRequest = new EditMessageRequest("92213476-74f8-4f63-8d39-55524e37099b", "bbdcc735-d897-45a7-b10d-62c57b52fcca", "Fonxy666", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/Message/EditMessage", messageChange);
        getUserResponse.EnsureSuccessStatusCode();
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