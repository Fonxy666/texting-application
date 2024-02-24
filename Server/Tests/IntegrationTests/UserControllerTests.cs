﻿using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class UserControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###", false);
    private readonly AuthRequest _testUser3 = new ("TestUsername3", "testUserPassword123###", false);

    public UserControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task Get_User_Credentials_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"/User/getUserCredentials?userId=20fa67ce-ec87-4518-b7b5-9bbdfcfebcce");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Get_User_Credentials_ReturnNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getUserResponse = await _client.GetAsync($"/User/getUserCredentials?username=NotFoundUserName");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Email_For_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test1@hotmail.com", "test1@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Change_Email_For_Not_Registered_User_Returns_NotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("notFound@gmail.com", "notFound@gmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Email_For_Not_2FA_User_Returns_NotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser3, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var emailRequest = new ChangeEmailRequest("test3@hotmail.com", "ntest3@hotmail.com");
        var jsonRequestRegister = JsonConvert.SerializeObject(emailRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangeEmail", userChangeEmail);
        Assert.Equal(HttpStatusCode.BadRequest, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Password_For_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var passwordRequest = new ChangeUserPasswordRequest("20fa67ce-ec87-4518-b7b5-9bbdfcfebcce", "testUserPassword123###", "testUserPassword123###!@#");
        var jsonRequestRegister = JsonConvert.SerializeObject(passwordRequest);
        var userChangeEmail = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/User/ChangePassword", userChangeEmail);
        getUserResponse.EnsureSuccessStatusCode();
        
        var passwordRequest1 = new ChangeUserPasswordRequest("20fa67ce-ec87-4518-b7b5-9bbdfcfebcce", "testUserPassword123###!@#", "testUserPassword123###");
        var jsonRequestRegister1 = JsonConvert.SerializeObject(passwordRequest1);
        var userChangeEmail1 = new StringContent(jsonRequestRegister1, Encoding.UTF8, "application/json");
        
        var getUserResponse1 = await _client.PatchAsync("/User/ChangePassword", userChangeEmail1);
        getUserResponse1.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetImage_With_Name_ReturnSuccessStatusCode()
    {
        const string imageName = "Fonxy666";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"User/GetImageWithUsername/{imageName}");

        getImageResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetImage_With_Id_ReturnSuccessStatusCode()
    {
        const string userId = "9de21f53-2295-4c67-81c5-5da58de27429";
        
        Directory.SetCurrentDirectory("D:/after codecool/texting-application/Server/MessageAppServer");
    
        var getImageResponse = await _client.GetAsync($"User/GetImage/{userId}");

        getImageResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Delete_User_ReturnSuccessStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client);

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        const string email = "test2@hotmail.com";
        const string username = "TestUsername2";
        const string password = "testUserPassword123###";

        var deleteUrl = $"/User/DeleteUser?email={Uri.EscapeDataString(email)}&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

        var getUserResponse = await _client.DeleteAsync(deleteUrl);
        getUserResponse.EnsureSuccessStatusCode();
    }
}