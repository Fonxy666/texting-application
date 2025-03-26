using UserService.Model.Requests.Auth;
using UserService.Model.Responses.Auth;

namespace UserService.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegistrationRequest request, string role, string imagePath);
    Task<AuthResult> LoginAsync(string username, bool rememberMe);
    Task<AuthResult> LoginWithExternal(string emailAddress);
    Task<AuthResult> ExamineLoginCredentials(string username, string password);
    Task<AuthResult> LogOut(string userId);
}