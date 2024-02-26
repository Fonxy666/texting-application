using System.Text;
using Newtonsoft.Json;
using Server.Requests.Auth;
using Server.Services.EmailSender;

namespace Tests;

public static class TestLogin
{
    public static async Task<string> Login_With_Test_User(AuthRequest request, HttpClient _client, string email)
    {
        var token = EmailSenderCodeGenerator.GenerateTokenForLogin(email);
        var login = new LoginAuth(request.UserName, request.RememberMe, token);
        var authJsonRequest = JsonConvert.SerializeObject(login);
        var authContent = new StringContent(authJsonRequest, Encoding.UTF8, "application/json");

        var authResponse = await _client.PostAsync("/Auth/Login", authContent);
        authResponse.EnsureSuccessStatusCode();

        var cookies = authResponse.Headers.GetValues("Set-Cookie");
        return string.Join("; ", cookies);
    }
}