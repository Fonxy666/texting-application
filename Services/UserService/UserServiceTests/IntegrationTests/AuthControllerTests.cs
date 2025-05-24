using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Textinger.Shared.Responses;
using UserService;
using UserService.Database;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Services.EmailSender;
using UserService.Services.PrivateKeyFolder;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace UserServiceTests.IntegrationTests;

[Collection("Sequential")]
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Startup>>, IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;
    private readonly MainDatabaseContext _context;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly IPrivateKeyService _keyService;
    private readonly HttpClient _vaultClient;

    public AuthControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("test-config.json")
            .Build();
        
        _vaultClient = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:8201")
        };
        
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(_configuration);
            })
            .ConfigureTestServices(services =>
            {
                var token = "root"; // for test only
                var address = "http://127.0.0.1:8201";

                services.RemoveAll<IPrivateKeyService>();
                services.AddScoped<IPrivateKeyService>(_ =>
                    new PrivateKeyService(_vaultClient!, token, address));
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
        _provider = _testServer.Host.Services;
        _context = _provider.GetRequiredService<MainDatabaseContext>();
        _keyService = _provider.GetRequiredService<IPrivateKeyService>();
    }
    
    public async Task InitializeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        PopulateDbAndAddRoles.AddRolesAndAdminSync(_provider, _configuration);
        PopulateDbAndAddRoles.CreateTestUsersSync(_provider, 5, _context);
    }

    public Task DisposeAsync()
    {
        _testServer.Dispose(); 
        return Task.CompletedTask;
    }

    /* [Fact]
    public async Task SaveKey()
    {
        var payload = new
        {
            data = new
            {
                private_key = "testKey",
                metadata = new
                {
                    created_by = "admin",
                    created_at = DateTime.UtcNow.ToString("o")
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(
            HttpMethod.Put,   // Use PUT
            "http://127.0.0.1:8201/v1/secret/data/private_keys/38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"
        );
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Vault-Token", "root");

        using var response = await _vaultClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        _testOutputHelper.WriteLine($"Vault response: {response.StatusCode}, Body: {body}");
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Login_With_Test()
    {
        var key = new PrivateKey("tesst", "test");
        var hehe = await _keyService.SaveKeyAsync(key, new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        _testOutputHelper.WriteLine(hehe.IsSuccess.ToString());
        var rawResponse = await _client.GetAsync("http://127.0.0.1:8201/v1/secret/data/private_keys/38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var json = await rawResponse.Content.ReadAsStringAsync();
        _testOutputHelper.WriteLine($"Raw Vault Response: {json}");
        var haha = await _keyService.GetEncryptedKeyByUserIdAsync(new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        _testOutputHelper.WriteLine(haha.IsSuccess.ToString());
        var token = EmailSenderCodeGenerator.GenerateShortToken("test1@hotmail.com", EmailType.Login);
        var login = new LoginAuth(_testUser.UserName, true, token);
        var authJsonRequest = JsonConvert.SerializeObject(login);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");

        var authResponse = await _client.PostAsync("api/v1/Auth/Login", authContent);
        authResponse.EnsureSuccessStatusCode();
    } */

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
            "-",
            "062922222221",
            "testPublicKey",
            "testPrivateKey",
            "testIv"
        );
        var jsonLoginRequest = JsonConvert.SerializeObject(testUser);
        var userLogin = new StringContent(jsonLoginRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PostAsync("api/v1/Auth/Register", userLogin);
        _testOutputHelper.WriteLine(getUserResponse.Content.ReadAsStringAsync().Result);
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
        var token = EmailSenderCodeGenerator.GenerateLongToken("test1@hotmail.com", EmailType.Registration);
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

        var token = EmailSenderCodeGenerator.GenerateLongToken("test1@hotmail.com", EmailType.Registration);
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
        var testUser = new AuthRequest("TestUsername5", "testUserPassword123###");
        var cookies = await TestLogin.Login_With_Test_User(testUser, _client, "test5@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var response = await _client.GetAsync($"api/v1/Auth/Logout");
        response.EnsureSuccessStatusCode();
    }
}