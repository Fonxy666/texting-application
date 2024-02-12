using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Server.Requests;
using Server.Responses;
using Server.Services.Authentication;
using Xunit;
using Assert = Xunit.Assert;
using System.Text.Json;
using Server.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tests.IntegrationTests;

public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient = factory.CreateClient();

    private async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    
    [Fact]
    public async Task Authenticate_Returns_OkResult_With_Valid_Credentials()
    {
        var request = new AuthRequest("Admin", "asdASDasd123666");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Login", content);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
    
    [Fact]
    public async Task Authenticate_Returns_BadRequest_With_Not_Valid_Credentials()
    {
        var request = new AuthRequest("Admin", "notValidPassword123");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Login", content);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }

    [Fact]
    public async Task Register_Returns_OkResult_With_Valid_Credentials()
    {
        var request = new RegistrationRequest("user@test.com", "test-user", "PasswordToTestUser", "06201234567",
            "");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Register", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
    
    [Fact]
    public async Task Register_Returns_BadRequest_With_Not_Valid_Credentials()
    {
        var request = new RegistrationRequest("", "test-user", "PasswordToTestUser", "06201234567",
            "");
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("http://localhost:5000/Auth/Register", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var authResponse = await DeserializeResponse<AuthResponse>(response);
        Assert.NotNull(authResponse);
    }
}