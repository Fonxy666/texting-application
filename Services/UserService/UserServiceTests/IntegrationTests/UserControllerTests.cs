// using System.Net;
// using System.Text;
// using AuthenticationService;
// using AuthenticationService.Model;
// using AuthenticationService.Model.Requests.Auth;
// using AuthenticationService.Model.Requests.User;
// using AuthenticationService.Model.Responses.User;
// using AuthenticationService.Services.EmailSender;
// using AuthenticationService.Services.FriendConnection;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.AspNetCore.TestHost;
// using Newtonsoft.Json;
// using Xunit;
// using Xunit.Abstractions;
// using Assert = Xunit.Assert;
//
// namespace Tests.IntegrationTests;
//
// [Collection("Sequential")]
// public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
// {
//     private readonly ITestOutputHelper _testOutputHelper;
//     private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
//     private readonly HttpClient _client;
//     private readonly TestServer _testServer;
//     private readonly UserManager<ApplicationUser> _userManager;
//     private readonly IFriendConnectionService _friendConnectionService;
//
//     public UserControllerTests(ITestOutputHelper testOutputHelper)
//     {
//         _testOutputHelper = testOutputHelper;
//         var builder = new WebHostBuilder()
//             .UseEnvironment("Test")
//             .UseStartup<Startup>()
//             .ConfigureAppConfiguration(config =>
//             {
//                 config.AddConfiguration(
//                     new ConfigurationBuilder()
//                         .SetBasePath(Directory.GetCurrentDirectory())
//                         .AddJsonFile("testConfiguration.json")
//                         .Build()
//                     );
//             });
//
//         _testServer = new TestServer(builder);
//         _friendConnectionService = _testServer.Services.GetRequiredService<IFriendConnectionService>();
//         _userManager = _testServer.Services.GetRequiredService<UserManager<ApplicationUser>>();
//         _client = _testServer.CreateClient();
//     }
//     
//     [Fact]
//     public async Task GetUserCredentials_ReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var getUserResponse = await _client.GetAsync($"api/v1/User/getUserCredentials");
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task ChangeEmail_WithValidModelState_ReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test1@hotmail123.com");
//         var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
//         getUserResponse.EnsureSuccessStatusCode();
//         
//         var emailRequest2 = new ChangeEmailRequest("test1@hotmail123.com", "test1@hotmail.com");
//         var jsonRequestRegister2 = JsonConvert.SerializeObject(emailRequest2);
//         var userChangeEmail2 = new StringContent(jsonRequestRegister2, Encoding.UTF8, "application/json");
//
//         var getUserResponse2 = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail2);
//         getUserResponse2.EnsureSuccessStatusCode();
//         
//         var responseContent = await getUserResponse.Content.ReadAsStringAsync();
//     
//         var responseObject = JsonConvert.DeserializeObject<EmailUsernameResponse>(responseContent);
//
//         Assert.NotNull(responseObject);
//     }
//     
//     [Fact]
//     public async Task ChangeEmail_ForNotActivated2FAUser_ReturnNotFound()
//     {
//         var user = new AuthRequest("TestUsername3", "testUserPassword123###");
//         var cookies = await TestLogin.Login_With_Test_User(user, _client, "test3@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var emailRequest = new ChangeEmailRequest("test3@hotmail.com", "ntest3@gmail.com");
//         var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
//         Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ChangeEmail_WithInUseUserEmail_ReturnNotNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test3@hotmail.com");
//         var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
//         Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ChangeEmail_WithNotValidEmail_ReturnBadRequest()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var emailRequest = new ChangeEmailRequest("test2@hotmail.com", "new@email.com");
//         var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeEmail", userChangeEmail);
//         
//         Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ChangePassword_ForValidUser_ReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var passwordRequest = new ChangePasswordRequest("testUserPassword123###", "testUserPassword123###!@#");
//         var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
//         getUserResponse.EnsureSuccessStatusCode();
//         
//         var passwordRequest1 = new ChangePasswordRequest("testUserPassword123###!@#", "testUserPassword123###");
//         var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
//         var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
//         
//         var getUserResponse1 = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail1);
//         getUserResponse1.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task ChangePassword_WithWrongPassword_ReturnNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(new AuthRequest("TestUsername2", "testUserPassword123###"), _client, "test2@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var passwordRequest = new ChangePasswordRequest("testUserPassword123", "testUserPassword123###!@#");
//         var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangePassword", userChangeEmail);
//         Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task DeleteUser_WithValidUser_ReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(new AuthRequest("TestUsername2", "testUserPassword123###"), _client, "test2@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string password = "testUserPassword123###";
//
//         var deleteUrl = $"api/v1/User/DeleteUser?password={Uri.EscapeDataString(password)}";
//
//         var getUserResponse = await _client.DeleteAsync(deleteUrl);
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task DeleteUser_WithWrongPassword_ReturnBadRequest()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string email = "test1@hotmail.com";
//         const string password = "123";
//
//         var deleteUrl = $"api/v1/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";
//
//         var getUserResponse = await _client.DeleteAsync(deleteUrl);
//         Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task GetUserName_WithValidId_ReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";
//
//         var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task GetUserName_WithNotValidId_ReturnsNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string userId = "123";
//
//         var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
//         Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task GetUserName_WithNotExistingId_ReturnNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa915";
//
//         var getUserResponse = await _client.GetAsync($"api/v1/User/GetUsername?userId={userId}");
//         Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ChangeAvatar_WithValidId_ReturnReturnSuccessStatusCode()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string request = "-";
//         var jsonRequestRegister = JsonConvert.SerializeObject(request);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//         
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task ChangeAvatar_WithInvalidUser_ReturnBadRequest()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string request = "image";
//         var jsonRequestRegister = JsonConvert.SerializeObject(request);
//         var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
//         
//         var getUserResponse = await _client.PatchAsync("api/v1/User/ChangeAvatar", userChangeEmail);
//         Assert.Equal(HttpStatusCode.InternalServerError, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ForgotPassword_WithValidEmail_ReturnSuccessStatusCode()
//     {
//         const string email = $"test1@hotmail.com";
//         
//         var getUserResponse = await _client.GetAsync($"api/v1/User/SendForgotPasswordToken?email={email}");
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task ForgotPassword_WitInvalidEmail_ReturnNotFound()
//     {
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//         _testOutputHelper.WriteLine(existingUser.UserName);
//         
//         const string email = "test@hotmail.com";
//         
//         var getUserResponse = await _client.GetAsync($"api/v1/User/SendForgotPasswordToken?email={email}");
//         Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task ExaminePasswordResetLink_WithValidEmail_ReturnSuccess()
//     {
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//     
//         var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
//     
//         EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);
//     
//         var encodedToken = token;
//         var getUserResponse = await _client.GetAsync($"api/v1/User/ExaminePasswordResetLink?email={existingUser.Email}&resetId={encodedToken}");
//     
//         getUserResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task ExaminePasswordResetLink_WithWrongEmail_ReturnBadRequest()
//     {
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//     
//         var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
//     
//         EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);
//     
//         var encodedToken = token;
//         var examineTokenResponse = await _client.GetAsync($"api/v1/User/ExaminePasswordResetLink?email=invalidemail@hotmail.com&resetId={encodedToken}");
//     
//         Assert.Equal(HttpStatusCode.BadRequest, examineTokenResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task SetNewPasswordAfterReset_WithValidEmail_ReturnSuccess()
//     {
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
//         var passwordResetRequest1 = new PasswordResetRequest(existingUser.Email, "testUserPassword123###asd");
//         var jsonRequest1 = JsonConvert.SerializeObject(passwordResetRequest1);
//         var resetPasswordJson1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         var token1 = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
//     
//         EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token1);
//
//         _testOutputHelper.WriteLine(existingUser.PasswordHash!);
//     
//         var getUserResponse1 = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={token1}", resetPasswordJson1);
//     
//         getUserResponse1.EnsureSuccessStatusCode();
//         
//         var passwordResetRequest2 = new PasswordResetRequest(existingUser.Email, "testUserPassword123###");
//         var jsonRequest2 = JsonConvert.SerializeObject(passwordResetRequest2);
//         var resetPasswordJson2 = new StringContent(jsonRequest2, Encoding.UTF8, "application/json");
//     
//         var token2 = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
//     
//         EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token2);
//     
//         var getUserResponse2 = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={token2}", resetPasswordJson2);
//     
//         getUserResponse2.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task SetNewPasswordAfterReset_WithWrongToken_ReturnBadRequest()
//     {
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
//         var passwordResetRequest = new PasswordResetRequest(existingUser.Email, "testUserPassword123###asd");
//         var jsonRequest = JsonConvert.SerializeObject(passwordResetRequest);
//         var resetPasswordJson = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
//     
//         var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
//         var wrongToken = "wrongtokenfortest";
//         if (wrongToken == null) throw new ArgumentNullException(nameof(wrongToken));
//
//         EmailSenderCodeGenerator.StorePasswordResetCode(existingUser.Email, token);
//
//         _testOutputHelper.WriteLine(existingUser.PasswordHash!);
//     
//         var getUserResponse = await _client.PostAsync($"api/v1/User/SetNewPassword?resetId={wrongToken}", resetPasswordJson);
//     
//         Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task SendFriendRequest_ReturnFriendResponse_AfterIt_SendAgain_ReturnBadRequest_ThanDeclineIt()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var jsonRequest = JsonConvert.SerializeObject("TestUsername2");
//         var sendFriendRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
//     
//         var sendFriendResponse = await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest);
//         sendFriendResponse.EnsureSuccessStatusCode();
//         
//         var sendFriendResponse2 = await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest);
//         Assert.Equal(HttpStatusCode.BadRequest, sendFriendResponse2.StatusCode);
//
//         var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2!.Id.ToString());
//         
//         var declineFriendRequestResponse = await _client.DeleteAsync($"api/v1/User/DeleteFriendRequest?requestId={requests!.ToList()[0].RequestId.ToString()}&userType=sender");
//         declineFriendRequestResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task SendFriendRequest_ToYourself_ReturnBadRequest()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         const string request = "TestUsername1";
//         var jsonRequest = JsonConvert.SerializeObject(request);
//         var sendFriendRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
//     
//         var getFriendResponse = await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest);
//     
//         Assert.Equal(HttpStatusCode.BadRequest, getFriendResponse.StatusCode);
//     }
//     
//     [Fact]
//     public async Task GetFriendRequestCount_ReturnRequests()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//     
//         var getFriendResponse = await _client.GetAsync($"api/v1/User/GetFriendRequestCount?userId={existingUser!.Id}");
//     
//         getFriendResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task DeclineFriendRequest_WithNotExistingUser_ReturnNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
//         var jsonRequest1 = JsonConvert.SerializeObject(request1);
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var declineFriendResponse1 = await _client.DeleteAsync($"api/v1/User/DeleteFriendRequest?requestId={Guid.NewGuid().ToString()}&userType=sender");
//     
//         Assert.Equal(HttpStatusCode.NotFound, declineFriendResponse1.StatusCode);
//         
//         var declineFriendResponse2 = await _client.DeleteAsync($"api/v1/User/DeleteFriendRequest?requestId={Guid.NewGuid().ToString()}&userType=sender");
//     
//         Assert.Equal(HttpStatusCode.NotFound, declineFriendResponse2.StatusCode);
//     }
//     
//     [Fact]
//     public async Task GetFriendRequests_WithExistingUser_ReturnFriends()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser1 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername1");
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var request1 = new FriendRequest(existingUser1.Id.ToString(), existingUser2.UserName);
//         var jsonRequest1 = JsonConvert.SerializeObject(request1);
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var getFriendsResponse = await _client.GetAsync($"api/v1/User/GetFriendRequests?userId={existingUser2.Id}");
//     
//         getFriendsResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task AcceptFriendRequest_WithExistingUser_ReturnOk()
//     {
//         var cookies1 = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies1);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername3");
//         var jsonRequest1 = JsonConvert.SerializeObject("TestUsername3");
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//         
//         _client.DefaultRequestHeaders.Remove("Cookie");
//         var cookies2 = await TestLogin.Login_With_Test_User(new AuthRequest("TestUsername3", "testUserPassword123###"), _client, "test3@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies2);
//         
//         var requestForAcceptWithInvalidId = Guid.NewGuid();
//         var jsonRequestForAcceptWithInvalidId = JsonConvert.SerializeObject(requestForAcceptWithInvalidId);
//         var requestWithInvalidId = new StringContent(jsonRequestForAcceptWithInvalidId, Encoding.UTF8, "application/json");
//         
//         var acceptResponseWithInvalidUser = await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", requestWithInvalidId);
//         Assert.Equal(HttpStatusCode.NotFound, acceptResponseWithInvalidUser.StatusCode);
//
//         var requestForAccept = requests.ToList()[0].RequestId.ToString();
//         var jsonRequestForAccept = JsonConvert.SerializeObject(requestForAccept);
//         var request = new StringContent(jsonRequestForAccept, Encoding.UTF8, "application/json");
//         
//         var acceptResponse = await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", request);
//         
//         acceptResponse.EnsureSuccessStatusCode();
//
//         await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={Uri.EscapeDataString(requestForAccept)}");
//     }
//     
//     [Fact]
//     public async Task DeleteSentFriendRequest_WithExistingUser_ReturnOk()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var jsonRequest1 = JsonConvert.SerializeObject("TestUsername2");
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var requests = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//         
//         var deleteSentFriendRequest = await _client.DeleteAsync($"api/v1/User/DeleteFriendRequest?requestId={Uri.EscapeDataString(Guid.NewGuid().ToString())}&userType=receiver");
//         Assert.Equal(HttpStatusCode.NotFound, deleteSentFriendRequest.StatusCode);
//         
//         var deleteResponse = await _client.DeleteAsync($"api/v1/User/DeleteFriendRequest?requestId={Uri.EscapeDataString(requests.ToList()[0].RequestId.ToString())}&userType=receiver");
//         
//         deleteResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task GetFriends_WithExistingUser_ReturnOk()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         
//         var getFriendsResponse = await _client.GetAsync($"api/v1/User/GetFriends?userId={existingUser2.Id}");
//         
//         getFriendsResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task Delete_WithExistingUser_ReturnOk_AfterIt_DeleteFriend_WithExistingId_And_WithNotExistingOne_FirstReturnOk_NotExisting_ReturnNotFound()
//     {
//         var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var jsonRequest1 = JsonConvert.SerializeObject("TestUsername2");
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var requests1 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//
//         var requestForAccept = requests1.ToList()[0].RequestId.ToString();
//         var jsonRequestForAccept = JsonConvert.SerializeObject(requestForAccept);
//         var request = new StringContent(jsonRequestForAccept, Encoding.UTF8, "application/json");
//         
//         await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", request);
//         
//         var requests2 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//         
//         var deleteFriendResponseWithInvalidConnectionId = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={Guid.NewGuid()}&userType=sender");
//         
//         Assert.Equal(HttpStatusCode.NotFound, deleteFriendResponseWithInvalidConnectionId.StatusCode);
//         
//         var deleteFriendResponse = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={requests2.ToList()[0].RequestId}&userType=sender");
//         
//         deleteFriendResponse.EnsureSuccessStatusCode();
//     }
//     
//     [Fact]
//     public async Task Delete_WithExistingUser_ButNotTheUsersFriends_ReturnBadRequest()
//     {
//         var cookies1 = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies1);
//         
//         var existingUser2 = _userManager.Users.FirstOrDefault(user => user.UserName == "TestUsername2");
//         var jsonRequest1 = JsonConvert.SerializeObject("TestUsername2");
//         var sendFriendRequest1 = new StringContent(jsonRequest1, Encoding.UTF8, "application/json");
//     
//         await _client.PostAsync("api/v1/User/SendFriendRequest", sendFriendRequest1);
//         
//         var requests1 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//
//         var requestForAccept = requests1.ToList()[0].RequestId.ToString();
//         var jsonRequestForAccept = JsonConvert.SerializeObject(requestForAccept);
//         var request = new StringContent(jsonRequestForAccept, Encoding.UTF8, "application/json");
//         
//         await _client.PatchAsync("api/v1/User/AcceptReceivedFriendRequest", request);
//         
//         var requests2 = await _friendConnectionService.GetPendingReceivedFriendRequests(existingUser2.Id.ToString());
//         
//         _client.DefaultRequestHeaders.Remove("Cookie");
//         var cookies2 = await TestLogin.Login_With_Test_User(new AuthRequest("TestUsername3", "testUserPassword123###"), _client, "test3@hotmail.com");
//         _client.DefaultRequestHeaders.Add("Cookie", cookies2);
//         
//         var deleteFriendResponseWithInvalidConnectionId = await _client.DeleteAsync($"api/v1/User/DeleteFriend?connectionId={requests2.ToList()[0].RequestId}&userType=sender");
//         
//         Assert.Equal(HttpStatusCode.BadRequest, deleteFriendResponseWithInvalidConnectionId.StatusCode);
//     }
// }