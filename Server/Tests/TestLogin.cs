using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Requests;
using Server.Requests.Auth;
using Server.Responses;

namespace Tests;

public static class TestLogin
{
    public static async Task<string> Login_With_Test_User(AuthRequest request, HttpClient _client)
    {
        var authJsonRequest = JsonConvert.SerializeObject(request);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");

        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        authResponse.EnsureSuccessStatusCode();

        var cookies = authResponse.Headers.GetValues("Set-Cookie");
        return string.Join("; ", cookies);
    }
}