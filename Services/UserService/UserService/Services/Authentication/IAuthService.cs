using UserService.Model.Requests;
using UserService.Model.Responses;

namespace UserService.Services.Authentication;

public interface IAuthService
{
    Task<ResponseBase> RegisterAsync(RegistrationRequest request, string role, string imagePath);
    Task<ResponseBase> LoginAsync(string username, bool rememberMe);
    Task<ResponseBase> LoginWithExternal(string emailAddress);
    Task<ResponseBase> ExamineLoginCredentials(string username, string password);
    Task<ResponseBase> LogOut(string userId);
}