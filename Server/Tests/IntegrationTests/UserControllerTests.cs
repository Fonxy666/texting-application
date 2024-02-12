using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Responses;
using Xunit;
using Xunit.Abstractions;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly RoomRequest _testRoom = new ("test", "test");

    public UserControllerTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
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
    public async Task Get_User_Credentials_ReturnSuccessStatusCode()
    {
        var loginResponse = await Login_With_Test_User(_testUser);
        
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");
        
        var jsonRequestRegister = JsonConvert.SerializeObject(_testUser.UserName);
        var getUser = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.GetAsync($"/User/getUserCredentials?username={_testUser.UserName}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Change_Email_For_User_ReturnSuccessStatusCode()
    {
        var loginResponse = await Login_With_Test_User(_testUser);
        
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");

        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test3@hotmail.com", loginResponse.Token);
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var reverseRequest = new ChangeEmailRequest("test3@hotmail.com", "test1@hotmail.com", loginResponse.Token);
        var reverseJsonRequestRegister = JsonConvert.SerializeObject(reverseRequest);
        var reverseUserChangeEmail = new StringContent(reverseJsonRequestRegister, Encoding.UTF8, "application/json");
        
        var reverseGetUserResponse = await _client.PatchAsync("/User/ChangeEmail", reverseUserChangeEmail);
        reverseGetUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Change_Password_For_User_ReturnSuccessStatusCode()
    {
        var loginResponse = await Login_With_Test_User(_testUser);
        
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");

        var passwordRequest = new ChangeUserPasswordRequest("viktor_6@windowslive.com", "asdASDasd123666", "asdASDasd123666!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangePassword", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var passwordRequest1 = new ChangeUserPasswordRequest("viktor_6@windowslive.com", "asdASDasd123666!@#", "asdASDasd123666");
        var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
        var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
        
        var getUserResponse1 = await _client.PatchAsync("/User/ChangePassword", userChangeEmail1);
        getUserResponse1.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetImage_ReturnSuccessStatusCode()
    {
        const string imageName = "Fonxy666";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"User/GetImage/{imageName}");

        getImageResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_User_ReturnSuccessStatusCode()
    {
        var loginResponse = await Login_With_Test_User(_testUser);
    
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");

        const string email = "test2@hotmail.com";
        const string username = "TestUsername2";
        const string password = "testUserPassword123###";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
}