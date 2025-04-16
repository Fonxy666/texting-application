using UserService.Models.Requests;
using UserService.Models.Responses;

namespace UserService.Services.Authentication;

public interface IAuthService
{
    Task<ResponseBase> RegisterAsync(RegistrationRequest request, string imagePath);
    Task<ResponseBase> LoginAsync(LoginAuth request);
    Task<ResponseBase> LoginWithExternal(string emailAddress);
    Task<ResponseBase> ExamineLoginCredentialsAsync(string username, string password);
    Task<ResponseBase> LogOutAsync(Guid userId);
}