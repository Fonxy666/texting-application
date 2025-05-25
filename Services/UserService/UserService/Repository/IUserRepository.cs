using System.Linq.Expressions;
using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Repository;

public interface IUserRepository
{
    Task<UserNameDto?> GetUsernameAsync(Guid userId);
    Task<UsernameUserEmailAndTwoFactorEnabledDto?> GetUsernameUserEmailAndTwoFactorEnabledAsync(Guid userId);
    Task<ApplicationUser?> GetUserWithIncludeAsync(Guid userId, Expression<Func<ApplicationUser, object>> includeNavigation);
    Task<UserPrivateKeyDto?> GetUserPrivateKeyAsync(Guid userId, Guid roomId);
    Task<UserPublicKeyDto?> GetUserPublicKeyAsync(string userName);
    Task<bool> IsUserHaveSymmetricKeyForRoom(string userName, Guid roomId);
}