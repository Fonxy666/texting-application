using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Responses;
using Xunit;

namespace Tests.IntegrationTests;

public class ChatControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly RoomRequest _testRoom = new("TestRoom1", "TestRoomPassword");

    public ChatControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    private async Task<AuthResponse> Login_With_Test_User(AuthRequest request)
    {
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        var responseContent = await authResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthResponse>(responseContent)!;
    }
    
    [Fact]
    public async Task ChatFunctions_ReturnSuccessStatusCode()
    {
        var client = _factory.CreateClient();
        var loginResponse = await Login_With_Test_User(_testUser);
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");
        
        var jsonRequestRegister = JsonConvert.SerializeObject(_testRoom);
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await client.PostAsync("/Chat/RegisterRoom", contentRegister);
        roomRegistrationResponse.EnsureSuccessStatusCode();

        var jsonRequest = JsonConvert.SerializeObject(_testRoom);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var roomLoginResponse = await client.PostAsync("/Chat/JoinRoom", content);
        roomLoginResponse.EnsureSuccessStatusCode();
        
        var jsonRequestDelete = JsonConvert.SerializeObject(_testRoom);
        var contentDelete = new StringContent(jsonRequestDelete, Encoding.UTF8, "application/json");

        var deleteRoomResponse = await client.PostAsync("/Chat/DeleteRoom", contentDelete);
        deleteRoomResponse.EnsureSuccessStatusCode();
    }
}