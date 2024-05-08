using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly AuthRequest _testUser3 = new ("TestUsername3", "testUserPassword123###");

    public UserControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    /*[Fact]
    public async Task GetUserCredentials_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"api/v1/User/getUserCredentials?userId=38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        getUserResponse.EnsureSuccessStatusCode();
    }*/
    
    /*[Fact]
    public async Task GetUserCredentials_ReturnSuccessStatusCode()
    {
        // Set the environment to Testing
        var webHostBuilder = new WebHostBuilder()
            .UseEnvironment("Test") // Set the environment to "Testing"
            .UseStartup<Startup>();

        // Load development configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json") // Use the development configuration
            .Build();

        // Override connection string for tests
        configuration["ConnectionString"] = "Server=localhost,1434;Database=textinger_test_database;User Id=sa;Password=yourStrong(!)Password;MultipleActiveResultSets=true;TrustServerCertificate=True";
        configuration["IssueAudience"] = "api With Authentication comes and goes here";
        configuration["ConnectionString"] = "Server=localhost,1433;Database=text_app;User Id=sa;Password=yourStrong(!)Password;MultipleActiveResultSets=true;TrustServerCertificate=True;";
        configuration["IssueSign"] = "V3ryStr0ngP@ssw0rdW1thM0reTh@n256B1ts";
        configuration["AdminEmail"] = "admin@gmail.com";
        configuration["AdminUserName"] = "Admin";
        configuration["AdminPassword"] = "asdASDasd123666$$$";
        configuration["DeveloperEmail"] = "new-smtp123-sender567@hotmail.com";
        configuration["DeveloperPassword"] = "asdASDasd123666$$$";
        configuration["GoogleClientId"] = "592491293143-mqspbv3b547k1r76rdg4l5i3ujm4kthm.apps.googleusercontent.com";
        configuration["GoogleClientSecret"] = "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";
        configuration["FacebookClientId"] = "388505830821078";
        configuration["FacebookClientSecret"] = "9348039785609456e379455b79d4cbb3";
        configuration["FrontendUrlAndPort"] = "http://localhost:4200";

        var factory = new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test"); // Set the environment to "Testing"
                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddConfiguration(configuration); // Load the modified configuration
                });
            });

        // Create the client
        var client = factory.CreateClient();
        
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await client.GetAsync("api/v1/User/getUserCredentials?userId=18c5eb4f-b614-45d0-9ee8-ad7f17e88dd9");
        getUserResponse.EnsureSuccessStatusCode();
    }*/
    
    [Fact]
    public async Task GetUser_Credentials_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"api/v1/User/getUserCredentials?username=NotFoundUserName");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithValidModelState_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test1@hotmail123.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var emailRequest2 = new ChangeEmailRequest("test1@hotmail123.com", "test1@hotmail.com");
        var jsonRequestRegister2 = JsonConvert.SerializeObject(emailRequest2);
        var userChangeEmail2 = new StringContent(jsonRequestRegister2, Encoding.UTF8, "application/json");

        var getUserResponse2 = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail2);
        getUserResponse2.EnsureSuccessStatusCode();
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
    
        var responseObject = JsonConvert.DeserializeObject<EmailUsernameResponse>(responseContent);

        Assert.NotNull(responseObject);
    }
    
    [Fact]
    public async Task ChangeEmail_WithNotRegisteredUser_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("notFound@gmail.com", "notFound@gmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_ForNotActivated2FAUser_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test3@hotmail.com", "ntest3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithInUserEmail_ReturnNotNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithNotValidEmail_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = "";
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_ForValidUser_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var passwordRequest1 = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###!@#", "testUserPassword123###", "testUserPassword123###");
        var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
        var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
        
        var getUserResponse1 = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail1);
        getUserResponse1.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangePassword_ForInvalidUser_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("123", "testUserPassword123###", "testUserPassword123###!@#", "otherTestUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithNotMatchingPasswords_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#", "testUserPassword123###!@#123");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetImage_WithValidId_ReturnSuccessStatusCode()
    {
        const string userId = "04b0b363-94b1-4c8a-4079-08dc6e8a212b";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"api/v1/User/GetImage?userId={userId}");

        getImageResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetImage_WithInvalidId_ReturnNotFound()
    {
        const string userId = "123";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"api/v1User/GetImage?userId={userId}");

        Assert.Equal(HttpStatusCode.NotFound, getImageResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteUser_WithValidUser_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "test2@hotmail.com";
        const string password = "testUserPassword123###";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_WithInvalidUser_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "123";
        const string password = "123";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteUser_WithWrongPassword_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "test1@hotmail.com";
        const string password = "123";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetUserName_WithValidId_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";

        var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetUserName_WithNotValidId_ReturnsNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string userId = "123";

        var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeAvatar_WithValidId_ReturnReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        var request = new AvatarChange("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "-");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangeAvatar_WithInvalidUser_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        var request = new AvatarChange("123", "image");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
}