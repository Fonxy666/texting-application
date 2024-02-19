using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Responses;
using Xunit;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");

    public AuthControllerTests(WebApplicationFactory<Startup> factory)
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
    public async Task Login_Test_User_ReturnSuccessStatusCode()
    {
        var request = new AuthRequest("TestUsername1", "testUserPassword123###");
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        authResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Register_Test_User_ReturnSuccessStatusCode()
    {
        var testUser = new RegistrationRequest("unique@hotmail.com", "uniqueTestUsername", "TestUserPassword", "01234567890", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("/Auth/Register", userLogin);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_User_ReturnSuccessStatusCode()
    {
        var loginResponse = await Login_With_Test_User(_testUser);
    
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResponse.Token}");

        const string email = "unique@hotmail.com";
        const string username = "uniqueTestUsername";
        const string password = "TestUserPassword";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
}