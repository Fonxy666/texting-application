namespace Server.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image);
    Task<AuthResult> LoginAsync(string username, string password);
}