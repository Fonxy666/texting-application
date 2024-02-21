using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Responses;

namespace Tests;

public static class TestLogin
{
    public static  async Task<AuthResponse> Login_With_Test_User(AuthRequest request, WebApplicationFactory<Startup> factory)
    {
        var client = factory.CreateClient();
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/Auth/Login", authContent);
        var responseContent = await authResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AuthResponse>(responseContent)!;
    }
}