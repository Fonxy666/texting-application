using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

public class CookiesControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public CookiesControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();

        configuration["ConnectionString"] = "Server=localhost,1434;Database=textinger_test_database;User Id=sa;Password=yourStrong(!)Password;MultipleActiveResultSets=true;TrustServerCertificate=True";
        configuration["IssueAudience"] = "api With Authentication for Tests correctly implemented";
        configuration["IssueSign"] = "V3ryStr0ngP@ssw0rdW1thM0reTh@n256B1ts4Th3T3sts";
        configuration["AdminEmail"] = "AdminEmail";
        configuration["AdminUserName"] = "AdminUserName";
        configuration["AdminPassword"] = "AdminPassword";
        configuration["DeveloperEmail"] = "DeveloperEmail";
        configuration["DeveloperPassword"] = "DeveloperPassword";
        configuration["GoogleClientId"] = "GoogleClientId";
        configuration["GoogleClientSecret"] = "GoogleClientSecret";
        configuration["FacebookClientId"] = "FacebookClientId";
        configuration["FacebookClientSecret"] = "FacebookClientSecret";
        configuration["FrontendUrlAndPort"] = "http://localhost:4200";
        
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

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Cookie/ChangeCookies", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}