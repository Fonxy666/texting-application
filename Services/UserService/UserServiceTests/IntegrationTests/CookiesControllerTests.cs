using System.Net;
using System.Text;
using AuthenticationService;
using AuthenticationService.Model.Requests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class CookiesControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public CookiesControllerTests()
    {
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
        var cookies = TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }
    
    [Fact]
    public async Task ChangeCookies_WithValidRequest_ReturnOkStatusCode()
    {
        var firstRequestData = new { request = "Animation" };
        var firstJsonRequest = JsonConvert.SerializeObject(firstRequestData);
        var firstContent = new StringContent(firstJsonRequest, Encoding.UTF8, "application/json");

        var firstResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=Animation", firstContent);
    
        var secondRequestData = new { request = "Anonymous" };
        var secondJsonRequest = JsonConvert.SerializeObject(secondRequestData);
        var secondContent = new StringContent(secondJsonRequest, Encoding.UTF8, "application/json");

        var secondResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=Anonymous", secondContent);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
    
    [Fact]
    public async Task ChangeCookies_WithInvalidParams_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject("asd");
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies?request=asd", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}