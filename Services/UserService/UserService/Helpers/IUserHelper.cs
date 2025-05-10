using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Helpers;

public interface IUserHelper
{
    Task<ResponseBase> GetUserOrFailureResponseAsync(
        UserIdentifierType type,
        string userCredential,
        Delegate onSuccess,
        Guid? roomId = null);
}