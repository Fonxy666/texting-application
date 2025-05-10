using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Helpers;

public class UserHelper(UserManager<ApplicationUser> userManager) : IUserHelper
{
    public async Task<ResponseBase> GetUserOrFailureResponseAsync(
    UserIdentifierType type,
    string userCredential,
    Delegate onSuccess,
    Guid? roomId
    )
    {
        Guid? userGuid = type is UserIdentifierType.UserId or 
            UserIdentifierType.UserIdIncludeReceivedRequest or 
            UserIdentifierType.UserIdIncludeSentRequest or 
            UserIdentifierType.UserIdIncludeFriends or
            UserIdentifierType.UserIdIncludeSymmetricKeys
            ? Guid.TryParse(userCredential, out var parsedGuid) ? parsedGuid : null
            : null;

        if ((int)type >= 1 && userGuid == null)
        {
            return new FailureWithMessage("Invalid user ID format.");
        }

        if (type == UserIdentifierType.UsernameExamineSymmetricKeys)
        {
            var boolResult = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .AnyAsync(u => u.UserName == userCredential && u.UserSymmetricKeys.Any(k => k.RoomId == roomId));

            return boolResult ? new Success() : new FailureWithMessage($"There is no key or user with this Username: {userCredential}");
        }

        var result = type switch
        {
            UserIdentifierType.Username => await userManager.FindByNameAsync(userCredential),
            UserIdentifierType.UserId => await userManager.FindByIdAsync(userCredential),
            UserIdentifierType.UserEmail => await userManager.FindByEmailAsync(userCredential),
            UserIdentifierType.UserIdIncludeReceivedRequest => await userManager.Users
                .Include(u => u.ReceivedFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential)),
            UserIdentifierType.UserIdIncludeSentRequest => await userManager.Users
                .Include(u => u.SentFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential)),
            UserIdentifierType.UserIdIncludeFriends => await userManager.Users
                .Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential)),
            UserIdentifierType.UserIdIncludeSymmetricKeys => await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential) && u.UserSymmetricKeys.Any(k => k.RoomId == roomId)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (result == null)
        {
            return new FailureWithMessage("User not found.");
        }

        if (onSuccess.Method.ReturnType == typeof(Task<ResponseBase>))
        {
            var resultAsync = await (Task<ResponseBase>)onSuccess.DynamicInvoke(result)!;
            return resultAsync;
        }
        var resultSync = (ResponseBase)onSuccess.DynamicInvoke(result)!;
        return resultSync;
    }
}