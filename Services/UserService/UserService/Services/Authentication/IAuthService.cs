using UserService.Models.Requests;
using Textinger.Shared.Responses;

namespace UserService.Services.Authentication;

public interface IAuthService
{
    Task<ResponseBase> RegisterAsync(RegistrationRequest request);
    Task<ResponseBase> LoginAsync(LoginAuth request);
    Task<ResponseBase> LoginWithExternal(string emailAddress);
    Task<ResponseBase> ExamineLoginCredentialsAsync(string userName, string password);
    Task<ResponseBase> LogOutAsync(Guid userId);
}