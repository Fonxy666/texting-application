using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly AuthRequest _testUser3 = new ("TestUsername3", "testUserPassword123###");

    public UserControllerTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Get_User_Credentials_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"/User/getUserCredentials?userId=38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Get_User_Credentials_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"/User/getUserCredentials?username=NotFoundUserName");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Email_For_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test1@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
    
        var responseObject = JsonConvert.DeserializeObject<EmailUsernameResponse>(responseContent);

        Assert.NotNull(responseObject);
        Assert.Equal("test1@hotmail.com", responseObject.Email);
        Assert.Equal("TestUsername1", responseObject.UserName);
    }
    
    [Fact]
    public async Task Change_Email_For_Not_Registered_User_Returns_NotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("notFound@gmail.com", "notFound@gmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Email_For_Not_2FA_User_Returns_NotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test3@hotmail.com", "ntest3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeEmail_WithNotValidEmail_ReturnBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client, "test3@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = "";
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        _testOutputHelper.WriteLine(getUserResponse.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Password_For_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangePassword", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var passwordRequest1 = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###!@#", "testUserPassword123###", "testUserPassword123###");
        var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
        var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
        
        var getUserResponse1 = await _client.PatchAsync("/User/ChangePassword", userChangeEmail1);
        getUserResponse1.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Change_Password_For_Invalid_User_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("123", "testUserPassword123###", "testUserPassword123###!@#", "otherTestUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Password_ReturnBadrequest_WithNotMatchingPasswords()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "testUserPassword123###", "testUserPassword123###!@#", "testUserPassword123###!@#123");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangePassword", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetImage_With_Id_ReturnSuccessStatusCode()
    {
        const string userId = "347045a9-f051-4a0e-aa93-3dfe59cd43c2";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"User/GetImage?userId={userId}");

        getImageResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetImage_With_InvalidId_ReturnNotFound()
    {
        const string userId = "123";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"User/GetImage?userId={userId}");

        Assert.Equal(HttpStatusCode.NotFound, getImageResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "test2@hotmail.com";
        const string password = "testUserPassword123###";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_Invalid_User_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "123";
        const string password = "123";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Delete_User_WithWrongPassword_Return_BadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "test1@hotmail.com";
        const string password = "123";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Get_User_Name_Valid_Id_Returns_Name()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string userId = "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916";

        var getUserResponse = await _client.GetAsync($"/User/GetUsername?userId={userId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Get_User_Name_Not_Valid_Id_Returns_NotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string userId = "123";

        var getUserResponse = await _client.GetAsync($"/User/GetUsername?userId={userId}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Avatar_Return_Ok()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        var request = new AvatarChange("347045a9-f051-4a0e-aa93-3dfe59cd43c2", "-");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("/User/ChangeAvatar", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Change_Avatar_With_Invalid_User_Return_Bad_Request()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        var request = new AvatarChange("123", "image");
        var jsonRequestRegister = JsonConvert.SerializeObject(request);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");
        
        var getUserResponse = await _client.PatchAsync("/User/ChangeAvatar", userChangeEmail);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
}