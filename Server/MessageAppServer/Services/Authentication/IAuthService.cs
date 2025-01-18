using AuthenticationServer.Model.Requests.Auth;
using AuthenticationServer.Model.Responses.Auth;

namespace AuthenticationServer.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegistrationRequest request, string role, string imagePath);
    Task<AuthResult> LoginAsync(string username, bool rememberMe);
    Task<AuthResult> LoginWithExternal(string emailAddress);
    Task<AuthResult> ExamineLoginCredentials(string username, string password);
    Task<AuthResult> LogOut(string userId);
}