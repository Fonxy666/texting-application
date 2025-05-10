using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Helpers;

public interface IUserHelper
{
    Task<T> GetUserOrFailureResponseAsync<T>(
        UserIdentifierType type,
        string userCredential,
        Delegate onSuccess,
        Func<string, T> onFailure,
        Guid? roomId = null);
}