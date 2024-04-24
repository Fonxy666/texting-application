/*using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Services.EmailSender;
using Xunit;
using Assert = Xunit.Assert;

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
    
    [Fact]
    public async Task Login_With_Invalid_User_ReturnBadRequestStatusCode()
    {
        var request = new AuthRequest("", "");
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }
    
    [Fact]
    public async Task Login_With_Bad_Credentials_ReturnBadRequest()
    {
        var token = EmailSenderCodeGenerator.GenerateTokenForLogin("test1@hotmail.com");
        var login = new LoginAuth("TestUs", false, token);
        var authJsonRequest = JsonConvert.SerializeObject(login);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }
    
    [Fact]
    public async Task Register_Test_User_ReturnSuccessStatusCode()
    {
        var testUser = new RegistrationRequest("unique@hotmail.com", "uniqueTestUsername", "TestUserPassword123666$$$", "01234567890", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("/Auth/Register", userLogin);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Register_Invalid_Test_User_ReturnBadRequest()
    {
        var testUser = new RegistrationRequest("", "", "", "", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("/Auth/Register", userLogin);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        const string email = "unique@hotmail.com";
        const string username = "uniqueTestUsername";
        const string password = "TestUserPassword123666$$$";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendEmailVerificationCode_ValidRequest_ReturnsOk()
    {
        var emailRequest = new GetEmailForVerificationRequest("test@test.hu");
        var jsonRequest = JsonConvert.SerializeObject(emailRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/Auth/GetEmailVerificationToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Get_VerifyToken_Valid_Code_ReturnsOk()
    {
        var request = new GetEmailForVerificationRequest("test1@hotmail.com");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/Auth/GetEmailVerificationToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Send_VerifyToken_Valid_Code_ReturnsOk()
    {
        var request = new AuthRequest(_testUser.UserName, _testUser.Password);
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/Auth/SendLoginToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Logout_Returns_BadRequest_With_Empty_Content()
    {
        var emptyContent = new StringContent("", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/Auth/Logout", emptyContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}*/