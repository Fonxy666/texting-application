using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Services.EmailSender;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public AuthControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
    public async Task Login_WithInvalidUser_ReturnBadRequestStatusCode()
    {
        var request = new AuthRequest("", "");
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("api/v1/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithBadCredentials_ReturnBadRequest()
    {
        var token = EmailSenderCodeGenerator.GenerateShortToken("test1@hotmail.com", "login");
        var login = new LoginAuth("TestUs", false, token);
        var authJsonRequest = JsonConvert.SerializeObject(login);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("api/v1/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithBadAuthToken_ReturnBadRequest()
    {
        var login = new LoginAuth(_testUser.UserName, false, "asd");
        var authJsonRequest = JsonConvert.SerializeObject(login);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("api/v1/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }
    
    [Fact]
    public async Task Register_TestUser_ReturnSuccessStatusCode()
    {
        var testUser = new RegistrationRequest(
            "unique@hotmail.com",
            "uniqueTestUsername",
            "TestUserPassword123666$$$",
            "",
            "06292222222",
            "testPublicKey",
            "testPrivateKey",
            "testIv"
        );
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("api/v1/Auth/Register", userLogin);
        getUserResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_InvalidTestUser_ReturnBadRequest()
    {
        var testUser = new RegistrationRequest("", "", "", "", "", "", "", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("api/v1/Auth/Register", userLogin);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }

    [Fact]
    public async Task Register_TestUserWithInvalidEmail_ReturnBadRequest()
    {
        var testUser = new RegistrationRequest("uniquaeotmail", "uniqueTestUsername123", "asdASD123%%%", "01234567890", "", "", "", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("api/v1/Auth/Register", userLogin);
        
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }

    [Fact]
    public async Task Register_TestUserWithInvalidPassword_ReturnBadRequest()
    {
        var testUser = new RegistrationRequest("uniquaeotmail@hotmail.com", "uniqueTestUsername123", "asdASD123", "01234567890", "", "", "", "");
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("api/v1/Auth/Register", userLogin);
        
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_TestUser_ReturnSuccessStatusCode()
    {
        var testUser = new AuthRequest("TestUsername5", "testUserPassword123###");
        var cookies = await TestLogin.Login_With_Test_User(testUser, _client, "test5@hotmail.com");

        const string password = "testUserPassword123###";

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.DeleteAsync($"api/v1/User/DeleteUser?password={Uri.EscapeDataString(password)}");
        getUserResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SendEmailVerificationCode_WithValidRequest_ReturnOk()
    {
        var emailRequest = new GetEmailForVerificationRequest("test@test.hu");
        var jsonRequest = JsonConvert.SerializeObject(emailRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/SendEmailVerificationToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendEmailVerificationCode_WithEmptyRequest_ReturnBadRequest()
    {
        var emailRequest = new GetEmailForVerificationRequest("");
        var jsonRequest = JsonConvert.SerializeObject(emailRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/SendEmailVerificationToken", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExamineVerifyToken_WithValidCode_ReturnOk()
    {
        var token = EmailSenderCodeGenerator.GenerateLongToken("test1@hotmail.com", "registration");
        var request = new VerifyTokenRequest("test1@hotmail.com", token);
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/ExamineVerifyToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamineVerifyToken_WithWrongToken_ReturnBadRequest()
    {
        _testOutputHelper.WriteLine("Starting test: ExamineVerifyToken_WithWrongToken_ReturnBadRequest");

        var token = EmailSenderCodeGenerator.GenerateLongToken("test1@hotmail.com", "registration");
        var request = new VerifyTokenRequest("test1@hotmail.com", "asd");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/ExamineVerifyToken", content);

        _testOutputHelper.WriteLine($"Response status: {response.StatusCode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Send_VerifyTokenWithValidCode_ReturnOk()
    {
        var request = new AuthRequest("TestUsername4", "testUserPassword123###");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/SendLoginToken", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Send_VerifyTokenWithWrongPassword_ReturnsBadRequest()
    {
        var request = new AuthRequest("TestUsername1", "asd");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("api/v1/Auth/SendLoginToken", content);


        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Return_Ok()
    {
        
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";

        var response = await _client.GetAsync($"api/v1/Auth/Logout");
        response.EnsureSuccessStatusCode();
    }
}