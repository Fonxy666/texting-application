using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MiddlewareTests;

[Collection("Sequential")]
public class MiddlewareTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");

    public MiddlewareTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidUser_ReturnBadRequestStatusCode()
    {
        var request = new AuthRequest("", "");
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }
}