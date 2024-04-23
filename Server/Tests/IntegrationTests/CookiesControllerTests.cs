using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Server;
using Server.Controllers;
using Server.Model.Requests.Auth;
using Server.Services.Cookie;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

public class CookiesControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");

    public CookiesControllerTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task ChangeCookies_WithValidRequest_ReturnsOkStatusCode()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var firstRequestData = new { request = "Animation" };
        var firstJsonRequest = JsonConvert.SerializeObject(firstRequestData);
        var firstContent = new StringContent(firstJsonRequest, Encoding.UTF8, "application/json");

        var firstResponse = await _client.PostAsync("http://localhost:7045/Cookie/ChangeCookies?request=Animation", firstContent);
    
        var secondRequestData = new { request = "Anonymous" };
        var secondJsonRequest = JsonConvert.SerializeObject(secondRequestData);
        var secondContent = new StringContent(secondJsonRequest, Encoding.UTF8, "application/json");

        var secondResponse = await _client.PostAsync("http://localhost:7045/Cookie/ChangeCookies?request=Anonymous", secondContent);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
    
    [Fact]
    public async Task Change_Cookies_Return_BadRequest_With_Invalid_Param()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com");

        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var jsonRequestRegister = JsonConvert.SerializeObject("asd");
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Cookie/ChangeCookies", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}