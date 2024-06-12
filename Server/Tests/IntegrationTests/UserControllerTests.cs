using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Server;
using Server.Model;
using Server.Model.Requests.Auth;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.EmailSender;
using Server.Services.FriendConnection;
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
    private readonly IFriendConnectionService _friendConnectionService;

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
        _friendConnectionService = _testServer.Services.GetRequiredService<IFriendConnectionService>();
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
        var passwordRequest = new ChangePasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var passwordRequest1 = new ChangePasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###!@#", "testUserPassword123###");
        var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
        var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
        
        var getUserResponse1 = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail1);
        getUserResponse1.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task ChangePassword_ForInvalidUser_ReturnInternalServerError()
    {
        var passwordRequest = new ChangePasswordRequest("123", "testUserPassword123###", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithNotExistingId_ReturnNotFound()
    {
        var passwordRequest = new ChangePasswordRequest(Guid.NewGuid().ToString(), "testUserPassword123###", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangePassword_WithWrongPassword_ReturnNotFound()
    {
        var passwordRequest = new ChangePasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
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
    public async Task GetUserName_WithNotExistingId_ReturnNotFound()
    {
        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa915";

        var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
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
    public async Task ChangeAvatar_WithNotExistingUser_ReturnNotFound()
    {
        var request = new AvatarChange(Guid.NewGuid().ToString(), "image");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
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
    
    [Fact]
    public async Task SetNewPasswordAfterReset_WithWrongToken_ReturnBadRequest()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
        var passwordResetRequest = new PasswordResetRequest(existingUser.Email, "testUserPassword123###asd");
        var jsonRequest = JsonConvert.SerializeObject(passwordResetRequest);
        var resetPasswordJson = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    
        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
        var wrongToken = "wrongtokenfortest";
        if (wrongToken == null) throw new ArgumentNullException(nameof(wrongToken));

        EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);

        _testOutputHelper.WriteLine(existingUser.PasswordHash!);
    
        var getUserResponse = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={wrongToken}", resetPasswordJson);
    
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendFriendRequest_ReturnFriendResponse_AfterIt_SendAgain_ReturnBadRequest_ThanDeclineIt()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        var request = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
        var jsonRequest = JsonConvert.SerializeObject(request);
        var sendFriendRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    
        var sendFriendResponse = await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest);
        sendFriendResponse.EnsureSuccessStatusCode();
        
        var sendFriendResponse2 = await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest);
        Assert.Equal(HttpStatusCode.BadRequest, sendFriendResponse2.StatusCode);

        var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());

        var requestForDecline = new FriendRequestManage(existingUser2.Id.ToString(), requests.ToList()[0].RequestId.ToString());
        var jsonRequestForDecline = JsonConvert.SerializeObject(requestForDecline);
        var declineFriendRequest = new StringContent(jsonRequestForDecline, Encoding.UTF8, "application/json");
        
        var declineFriendRequestResponse = await _client.PatchAsync($"api/v1/User/DeclineReceivedFriendRequest", declineFriendRequest);
        declineFriendRequestResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendFriendRequest_WithNotExistingUser_ReturnNotFound()
    {
        var request = new FriendRequest(Guid.NewGuid().ToString(), "TestUsername1");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var sendFriendRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    
        var getFriendResponse = await _client.PostAsync($"api/v1/User/SendFriendRequest", sendFriendRequest);
    
        Assert.Equal(HttpStatusCode.NotFound, getFriendResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendFriendRequest_ToYourself_ReturnBadRequest()
    {
        var request = new FriendRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "TestUsername1");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var sendFriendRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    
        var getFriendResponse = await _client.PostAsync($"api/v1/User/SendFriendRequest", sendFriendRequest);
    
        Assert.Equal(HttpStatusCode.BadRequest, getFriendResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetFriendRequestCount_ReturnRequests()
    {
        var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
    
        var getFriendResponse = await _client.GetAsync($"api/v1/User/GetFriendRequestCount?userId={existingUser.Id}");
    
        getFriendResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetFriendRequestCount_WithNotExistingUser_ReturnNotFound()
    {
        var getFriendResponse = await _client.GetAsync($"api/v1/User/GetFriendRequestCount?userId={Guid.NewGuid()}");
    
        Assert.Equal(HttpStatusCode.NotFound, getFriendResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeclineFriendRequest_WithNotExistingUser_ReturnNotFound()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
        var jsonRequest1 = JsonConvert.SerializeObject(request1);
        var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
        
        var request2 = new FriendRequestManage(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var jsonRequest2 = JsonConvert.SerializeObject(request2);
        var declineFriendRequest1 = new StringContent(jsonRequest2, Encoding.UTF8, "application/json");
        
        var declineFriendResponse1 = await _client.PatchAsync($"api/v1/User/DeclineReceivedFriendRequest", declineFriendRequest1);
    
        Assert.Equal(HttpStatusCode.NotFound, declineFriendResponse1.StatusCode);
        
        var request3 = new FriendRequestManage(existingUser2.Id.ToString(), Guid.NewGuid().ToString());
        var jsonRequest3 = JsonConvert.SerializeObject(request3);
        var declineFriendRequest2 = new StringContent(jsonRequest3, Encoding.UTF8, "application/json");
        
        var declineFriendResponse2 = await _client.PatchAsync($"api/v1/User/DeclineReceivedFriendRequest", declineFriendRequest2);
    
        Assert.Equal(HttpStatusCode.NotFound, declineFriendResponse2.StatusCode);
    }
    
    [Fact]
    public async Task GetFriendRequests_WithExistingUser_ReturnFriends()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
        var jsonRequest1 = JsonConvert.SerializeObject(request1);
        var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
        
        var getFriendsWrongResponse = await _client.GetAsync($"api/v1/User/GetFriendRequests?userId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, getFriendsWrongResponse.StatusCode);
        
        var getFriendsResponse = await _client.GetAsync($"api/v1/User/GetFriendRequests?userId={existingUser2.Id}");
    
        getFriendsResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task AcceptFriendRequest_WithExistingUser_ReturnOk()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
        var jsonRequest1 = JsonConvert.SerializeObject(request1);
        var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
        
        var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
        
        var requestForAcceptWithInvalidId = new FriendRequestManage(Guid.NewGuid().ToString(), requests.ToList()[0].RequestId.ToString());
        var jsonRequestForAcceptWithInvalidId = JsonConvert.SerializeObject(requestForAcceptWithInvalidId);
        var requestWithInvalidId = new StringContent(jsonRequestForAcceptWithInvalidId, Encoding.UTF8, "application/json");
        
        var acceptResponseWithInvalidUser = await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", requestWithInvalidId);
        Assert.Equal(HttpStatusCode.NotFound, acceptResponseWithInvalidUser.StatusCode);
        
        var requestForAcceptWithInvalidId2 = new FriendRequestManage(existingUser2.Id.ToString(), Guid.NewGuid().ToString());
        var jsonRequestForAcceptWithInvalidId2 = JsonConvert.SerializeObject(requestForAcceptWithInvalidId2);
        var requestWithInvalidId2 = new StringContent(jsonRequestForAcceptWithInvalidId2, Encoding.UTF8, "application/json");
        
        var acceptResponseWithInvalidUser2 = await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", requestWithInvalidId2);
        Assert.Equal(HttpStatusCode.NotFound, acceptResponseWithInvalidUser2.StatusCode);

        var requestForAccept = new FriendRequestManage(existingUser2.Id.ToString(), requests.ToList()[0].RequestId.ToString());
        var jsonRequestForAccept = JsonConvert.SerializeObject(requestForAccept);
        var request = new StringContent(jsonRequestForAccept, Encoding.UTF8, "application/json");
        
        var acceptResponse = await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", request);
        
        acceptResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteSentFriendRequest_WithExistingUser_ReturnOk()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
        var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
        var jsonRequest1 = JsonConvert.SerializeObject(request1);
        var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
        
        var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
        
        var deleteSentFriendRequest = await _client.DeleteAsync($"api/v1/User/DeleteSentFriendRequest?requestId={requests.ToList()[0].RequestId.ToString()}&userId={Guid.NewGuid().ToString()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteSentFriendRequest.StatusCode);
        
        var deleteResponseWithInvalidUser2 = await _client.DeleteAsync($"api/v1/User/DeleteSentFriendRequest?requestId={Guid.NewGuid().ToString()}&userId={existingUser2.Id.ToString()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponseWithInvalidUser2.StatusCode);
        
        var deleteResponse = await _client.DeleteAsync($"api/v1/User/DeleteSentFriendRequest?requestId={requests.ToList()[0].RequestId.ToString()}&userId={existingUser2.Id.ToString()}");
        
        deleteResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetFriends_WithExistingUser_ReturnOk()
    {
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        
        var getFriendsResponse = await _client.GetAsync($"api/v1/User/GetFriends?userId={existingUser2.Id}");
        
        getFriendsResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetFriends_WithNotExistingUser_NotFound()
    {
        var getFriendsResponse = await _client.GetAsync($"api/v1/User/GetFriends?userId={Guid.NewGuid()}");
        
        Assert.Equal(HttpStatusCode.NotFound, getFriendsResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_WithExistingUser_ReturnOk_AfterIt_DeleteFriend_WithExistingId_And_WithNotExistingOne_FirstReturnOk_NotExisting_ReturnNotFound()
    {
        var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
        var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
        var request1 = new FriendRequest(existingUser2.Id.ToString(), existingUser1.UserName);
        var jsonRequest1 = JsonConvert.SerializeObject(request1);
        var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
    
        await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
        
        var requests1 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser1.Id.ToString());

        var requestForAccept = new FriendRequestManage(existingUser2.Id.ToString(), requests1.ToList()[0].RequestId.ToString());
        var jsonRequestForAccept = JsonConvert.SerializeObject(requestForAccept);
        var request = new StringContent(jsonRequestForAccept, Encoding.UTF8, "application/json");
        
        await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", request);
        
        var requests2 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser1.Id.ToString());
        
        var deleteFriendResponseWithInvalidUser = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={requests2.ToList()[0].RequestId}&userId={Guid.NewGuid()}");
        
        Assert.Equal(HttpStatusCode.BadRequest, deleteFriendResponseWithInvalidUser.StatusCode);
        
        var deleteFriendResponseWithInvalidConnectionId = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={Guid.NewGuid()}&userId={existingUser1.Id}");
        
        Assert.Equal(HttpStatusCode.NotFound, deleteFriendResponseWithInvalidConnectionId.StatusCode);
        
        var deleteFriendResponse = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={requests2.ToList()[0].RequestId}&userId={existingUser1.Id}");
        
        deleteFriendResponse.EnsureSuccessStatusCode();
    }
}