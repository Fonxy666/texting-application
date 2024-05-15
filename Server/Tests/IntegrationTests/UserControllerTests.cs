using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Server;
using Server.Model;
using Server.Model.Requests.Auth;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.EmailSender;
using Tests.Services.Auth;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(
                    new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("testConfiguration.json")
                        .Build()
                    );
            });

        _testServer = new TestServer(builder);
        _userManager = _testServer.Services.GetRequiredService<UserManager<ApplicationUser>>();
        _client = _testServer.CreateClient();
        
        var cookies = TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }
    
    [Fact]
    public async Task GetUserCredentials_ReturnSuccessStatusCode()
    {
        var getUserResponse = await _client.GetAsync($"api/v1/User/getUserCredentials?userId=38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetUser_Credentials_ReturnNotFound()
    {
        var getUserResponse = await _client.GetAsync($"api/v1/User/getUserCredentials?username=NotFoundUserName");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithValidModelState_ReturnSuccessStatusCode()
    {
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
        var emailRequest = new ChangeEmailRequest("notFound@gmail.com", "notFound@gmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_ForNotActivated2FAUser_ReturnNotFound()
    {
        var emailRequest = new ChangeEmailRequest("test3@hotmail.com", "ntest3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithInUseUserEmail_ReturnNotNotFound()
    {
        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithNotValidEmail_ReturnBadRequest()
    {
        var emailRequest = "";
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
        
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_ForValidUser_ReturnSuccessStatusCode()
    {
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
        var passwordRequest = new ChangeUserPasswordRequest("123", "testUserPassword123###", "testUserPassword123###!@#", "otherTestUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithNotMatchingPasswords_ReturnBadRequest()
    {
        var passwordRequest = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#", "testUserPassword123###!@#123");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    /*[Fact]
    public async Task GetImage_WithValidId_ReturnSuccessStatusCode()
    {
        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";
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
    }*/
    
    [Fact]
    public async Task DeleteUser_WithValidUser_ReturnSuccessStatusCode()
    {
        const string email = "test2@hotmail.com";
        const string password = "testUserPassword123###";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_WithInvalidUser_ReturnBadRequest()
    {
        const string email = "123";
        const string password = "123";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteUser_WithWrongPassword_ReturnBadRequest()
    {
        const string email = "test1@hotmail.com";
        const string password = "123";

        var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetUserName_WithValidId_ReturnSuccessStatusCode()
    {
        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";

        var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetUserName_WithNotValidId_ReturnsNotFound()
    {
        const string userId = "123";

        var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeAvatar_WithValidId_ReturnReturnSuccessStatusCode()
    {
        var request = new AvatarChange("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "-");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangeAvatar_WithInvalidUser_ReturnBadRequest()
    {
        var request = new AvatarChange("123", "image");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnSuccessStatusCode()
    {
        const string email = $"test1@hotmail.com";
        
        var getUserResponse = await _client.GetAsync($"api/v1/User/SendForgotPasswordToken?email={email}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ForgotPassword_WitInvalidEmail_ReturnNotFound()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        _testOutputHelper.WriteLine(existingUser.UserName);
        
        const string email = "test@hotmail.com";
        
        var getUserResponse = await _client.GetAsync($"api/v1/User/SendForgotPasswordToken?email={email}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ExaminePasswordResetLink_WithValidEmail_ReturnSuccess()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
    
        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
    
        EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);
    
        var encodedToken = token;
        var getUserResponse = await _client.GetAsync($"api/v1/User/ExaminePasswordResetLink?email={existingUser.Email}&resetId={encodedToken}");
    
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ExaminePasswordResetLink_WithWrongEmail_ReturnBadRequest()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
    
        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
    
        EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);
    
        var encodedToken = token;
        var examineTokenResponse = await _client.GetAsync($"api/v1/User/ExaminePasswordResetLink?email=invalidemail@hotmail.com&resetId={encodedToken}");
    
        Assert.Equal(HttpStatusCode.BadRequest, examineTokenResponse.StatusCode);
    }
    
    [Fact]
    public async Task SetNewPasswordAfterReset_WithValidEmail_ReturnSuccess()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
        var passwordResetRequest1 = new PasswordResetRequest(existingUser.Email, "testUserPassword123###asd");
        var jsonRequest1 = JsonConvert.SerializeObject(passwordResetRequest1);
        var resetPasswordJson1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        var token1 = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
    
        EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token1);

        _testOutputHelper.WriteLine(existingUser.PasswordHash!);
    
        var getUserResponse1 = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={token1}", resetPasswordJson1);
    
        getUserResponse1.EnsureSuccessStatusCode();
        
        var passwordResetRequest2 = new PasswordResetRequest(existingUser.Email, "testUserPassword123###");
        var jsonRequest2 = JsonConvert.SerializeObject(passwordResetRequest2);
        var resetPasswordJson2 = new StringContent(jsonRequest2, Encoding.UTF8, "application/json");
    
        var token2 = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
    
        EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token2);
    
        var getUserResponse2 = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={token2}", resetPasswordJson2);
    
        getUserResponse2.EnsureSuccessStatusCode();
    }
}