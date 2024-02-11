using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Responses;
using Xunit;
using Xunit.Abstractions;

namespace Tests.IntegrationTests;

public class RealDatabaseChatControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername", "testUserPassword123###");

    public RealDatabaseChatControllerTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
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
        _testOutputHelper.WriteLine(loginResponse.ToString());
        
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");
        
        var roomRequestRegister = new RoomRequest("TestRoom1", "TestRoomPassword");
        var jsonRequestRegister = JsonConvert.SerializeObject(roomRequestRegister);
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        await client.PostAsync("/Chat/RegisterRoom", contentRegister);

        var roomRequest = new RoomRequest("TestRoom1", "TestRoomPassword");
        var jsonRequest = JsonConvert.SerializeObject(roomRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/Chat/JoinRoom", content);

        response.EnsureSuccessStatusCode();
        
        var roomRequestDelete = new RoomRequest("TestRoom1", "TestRoomPassword");
        var jsonRequestDelete = JsonConvert.SerializeObject(roomRequestDelete);
        var contentDelete = new StringContent(jsonRequestDelete, Encoding.UTF8, "application/json");

        await client.PostAsync("/Chat/DeleteRoom", contentDelete);
    }
}