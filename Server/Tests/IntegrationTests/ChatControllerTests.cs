using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Chat;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class ChatControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly RoomRequest _testRoom = new ("TestRoom1", "TestRoomPassword");

    public ChatControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task ChatFunctions_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");
        
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(_testRoom);
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Chat/RegisterRoom", contentRegister);
        roomRegistrationResponse.EnsureSuccessStatusCode();

        var jsonRequest = JsonConvert.SerializeObject(_testRoom);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var roomLoginResponse = await _client.PostAsync("/Chat/JoinRoom", content);
        roomLoginResponse.EnsureSuccessStatusCode();
        
        var jsonRequestDelete = JsonConvert.SerializeObject(_testRoom);
        var contentDelete = new StringContent(jsonRequestDelete, Encoding.UTF8, "application/json");

        var deleteRoomResponse = await _client.PostAsync("/Chat/DeleteRoom", contentDelete);
        deleteRoomResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Create_Room_Taken_RoomName_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "test"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task Create_Room_Invalid_Credentials_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }

    [Fact]
    public async Task Join_Room_Invalid_Credentials_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_Room_Invalid_Credentials_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Chat/DeleteRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}