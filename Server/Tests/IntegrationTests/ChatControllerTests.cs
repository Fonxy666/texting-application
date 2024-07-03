using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly AuthRequest _testUser2 = new ("TestUsername3", "testUserPassword123###");
    private readonly RoomRequest _testRoom = new ("TestRoom1", "TestRoomPassword");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public ChatControllerTests()
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
    public async Task ChatFunctions_ReturnSuccessStatusCode()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(_testRoom);
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        roomRegistrationResponse.EnsureSuccessStatusCode();

        var jsonRequest = JsonConvert.SerializeObject(new RoomRequest(_testRoom.RoomName, _testRoom.Password));
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var roomLoginResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", content);
        
        roomLoginResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task CreateRoom_WithTakenRoomName_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "test"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task ExamineUserIsTheCreator_WithValidParams_ReturnSuccess()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a9";

        var examineRoomCreatorResponse = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={roomId}");
        examineRoomCreatorResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ExamineUserIsTheCreator_WithNotExistingRoomId_NotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a4";

        var examineRoomCreatorResponse = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={roomId}");
        Assert.Equal(HttpStatusCode.NotFound, examineRoomCreatorResponse.StatusCode);
    }
    
    [Fact]
    public async Task ExamineUserIsTheCreator_WithNotExistingUserId_NotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a8";

        var examineRoomCreatorResponse = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={roomId}");
        Assert.Equal(HttpStatusCode.NotFound, examineRoomCreatorResponse.StatusCode);
    }
    
    [Fact]
    public async Task ExamineUserIsTheCreator_WithValidButNotCreatorId_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser2, _client, "test3@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a9";

        var examineRoomCreatorResponse = await _client.GetAsync($"api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId={roomId}");
        Assert.Equal(HttpStatusCode.BadRequest, examineRoomCreatorResponse.StatusCode);
    }
    
    [Fact]
    public async Task CreateRoom_WithInvalidCredentials_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WithInvalidCredentials_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var joinRoomResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, joinRoomResponse.StatusCode);
    }
    
    [Fact]
    public async Task JoinRoom_WithInvalidCredentials_ReturnNotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("wrongRoomName", "wrongRoomPassword"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var joinRoomResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.NotFound, joinRoomResponse.StatusCode);
    }
    
    [Fact]
    public async Task JoinRoom_WithInvalidPassword_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "asd"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var joinRoomResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, joinRoomResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithNotExistingRoom_ReturnNotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";
        const string roomId = "801d40c6-c95d-47ed-a21a-88cda341d0a7";

        var deleteRoomResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?userId={userId}&roomId={roomId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteRoomResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithNotExistingUser_ReturnNotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var userId = Guid.NewGuid().ToString();
        const string roomId = "801d40c6-c95d-47ed-a21a-88cda341d0a9";

        var deleteRoomResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?userId={userId}&roomId={roomId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteRoomResponse.StatusCode);
    }
    
    [Fact, Order(1)]
    public async Task DeleteRoom_NotCreatorUserId_ReturnBadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser2, _client, "test3@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a9";

        var deleteRoomResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?roomId={roomId}");
        Assert.Equal(HttpStatusCode.BadRequest, deleteRoomResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithValidCredentials_ReturnSuccess()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";
        const string roomId = "801d40c6-c95d-47ed-a21a-88cda341d0a9";

        var deleteRoomResponse = await _client.DeleteAsync($"api/v1/Chat/DeleteRoom?userId={userId}&roomId={roomId}");
        deleteRoomResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangePassword_WithValidRequest_ReturnSuccess()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var changeRoomPasswordRequest1 = new ChangeRoomPassword("901d40c6-c95d-47ed-a21a-88cda341d0a9", "test", "testtest");
        var jsonChangePassword1 = JsonConvert.SerializeObject(changeRoomPasswordRequest1);
        var contentChangePassword1 = new StringContent(jsonChangePassword1, Encoding.UTF8, "application/json");

        var changePasswordResponse1 = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword1);
        changePasswordResponse1.EnsureSuccessStatusCode();
        
        var changeRoomPasswordRequest2 = new ChangeRoomPassword("901d40c6-c95d-47ed-a21a-88cda341d0a9", "testtest", "test");
        var jsonChangePassword2 = JsonConvert.SerializeObject(changeRoomPasswordRequest2);
        var contentChangePassword2 = new StringContent(jsonChangePassword2, Encoding.UTF8, "application/json");

        var changePasswordResponse2 = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword2);
        changePasswordResponse2.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangePassword_WithInvalidRequest_BadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var changeRoomPasswordRequest = new ChangeRoomPassword("", "", "");
        var jsonChangePassword = JsonConvert.SerializeObject(changeRoomPasswordRequest);
        var contentChangePassword = new StringContent(jsonChangePassword, Encoding.UTF8, "application/json");

        var changePasswordResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword);
        Assert.Equal(HttpStatusCode.BadRequest, changePasswordResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithNotExistingRoom_ReturnNotFound()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var changeRoomPasswordRequest = new ChangeRoomPassword("901d40c6-c95d-47ed-a21a-88cda341d0a8", "test", "testtest");
        var jsonChangePassword = JsonConvert.SerializeObject(changeRoomPasswordRequest);
        var contentChangePassword = new StringContent(jsonChangePassword, Encoding.UTF8, "application/json");

        var changePasswordResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword);
        Assert.Equal(HttpStatusCode.NotFound, changePasswordResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithNotMatchingPassword_BadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var changeRoomPasswordRequest = new ChangeRoomPassword("901d40c6-c95d-47ed-a21a-88cda341d0a9", "testNotMatching", "testtest");
        var jsonChangePassword = JsonConvert.SerializeObject(changeRoomPasswordRequest);
        var contentChangePassword = new StringContent(jsonChangePassword, Encoding.UTF8, "application/json");

        var changePasswordResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword);
        Assert.Equal(HttpStatusCode.BadRequest, changePasswordResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithInvaliduser_BadRequest()
    {
        var cookies = TestLogin.Login_With_Test_User(_testUser2, _client, "test3@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var changeRoomPasswordRequest = new ChangeRoomPassword("901d40c6-c95d-47ed-a21a-88cda341d0a9", "test", "testtest");
        var jsonChangePassword = JsonConvert.SerializeObject(changeRoomPasswordRequest);
        var contentChangePassword = new StringContent(jsonChangePassword, Encoding.UTF8, "application/json");

        var changePasswordResponse = await _client.PatchAsync("api/v1/Chat/ChangePasswordForRoom", contentChangePassword);
        Assert.Equal(HttpStatusCode.BadRequest, changePasswordResponse.StatusCode);
    }
}