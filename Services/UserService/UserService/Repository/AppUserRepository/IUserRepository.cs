using System.Linq.Expressions;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;

namespace UserService.Repository.AppUserRepository;

public interface IUserRepository
{
    Task<UserNameDto?> GetUsernameDtoAsync(Guid userId);
    Task<UserNameDto?> GetUsernameDtoAsync(string userName);
    Task<UserIdDto?> GetUserIdDtoAsync(string userName);
    Task<UsernameUserEmailAndTwoFactorEnabledDto?> GetUsernameUserEmailAndTwoFactorEnabledAsync(Guid userId);
    Task<ApplicationUser?> GetUserWithIncludeAsync(Guid userId, Expression<Func<ApplicationUser, object>> includeNavigation);
    Task<UserPrivateKeyDto?> GetUserPrivateKeyAsync(Guid userId, Guid roomId);
    Task<UserPublicKeyDto?> GetUserPublicKeyAsync(string userName);
    Task<bool> IsUserHaveSymmetricKeyForRoomAsync(string userName, Guid roomId);
    Task<ApplicationUser?> ValidateUserInputAsync(RegistrationRequest request);
    Task<bool> IsUserExistingAsync(Guid userId);
    Task<UserIdAndPublicKeyDto?> GetUserIdAndPublicKeyAsync(Guid userId);
    Task<UserIdAndUserNameDto?> GetUserIdAndUserNameAsync(string userName);
}