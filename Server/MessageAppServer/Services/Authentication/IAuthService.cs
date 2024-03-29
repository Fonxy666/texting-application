using Server.Responses;
using Server.Responses.Auth;
using Server.Responses.User;

namespace Server.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image);
    Task<AuthResult> LoginAsync(string username, bool rememberMe);
    void ChangeCookies(string cookieName);
    Task<string?> GetEmailFromUserName(string username);
    Task<AuthResult> ExamineLoginCredentials(string username, string password);
    Task<AuthResult> LogOut(string userId);
    Task<DeleteUserResponse> DeleteAsync(string username, string password);
}