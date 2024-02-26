using Server.Responses;
using Server.Responses.User;

namespace Server.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image);
    Task<AuthResult> LoginAsync(string username, string password, bool rememberMe);
    Task<string?> GetEmailFromUserName(string username);
    Task<AuthResult> ExamineLoginCredentials(string username, string password, bool rememberMe);
    Task<AuthResult> LogOut();
    Task<DeleteUserResponse> DeleteAsync(string username, string password);
}