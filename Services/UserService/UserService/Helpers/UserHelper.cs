using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Helpers;

public class UserHelper(UserManager<ApplicationUser> userManager) : IUserHelper
{
    public async Task<T> GetUserOrFailureResponseAsync<T>(
        UserIdentifierType type,
        string userCredential,
        Delegate onSuccess,
        Func<string, T> onFailure,
        Guid? roomId)
    {
        Guid? userGuid = type is UserIdentifierType.UserId or 
                         UserIdentifierType.UserIdIncludeReceivedRequest or 
                         UserIdentifierType.UserIdIncludeSentRequest or 
                         UserIdentifierType.UserIdIncludeFriends or
                         UserIdentifierType.UserIdIncludeSymmetricKeys
                         ? Guid.TryParse(userCredential, out var parsedGuid) ? parsedGuid : null
                         : null;

        if ((int)type >= 1 && userGuid == null)
            return onFailure("Invalid user ID format.");

        if (type == UserIdentifierType.UsernameExamineSymmetricKeys)
        {
            var ok = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .AnyAsync(u => u.UserName == userCredential && u.UserSymmetricKeys.Any(k => k.RoomId == roomId));

            return ok ? (T)onSuccess.DynamicInvoke(new ApplicationUser { UserName = userCredential })! 
                      : onFailure($"There is no key or user with this Username: {userCredential}");
        }

        var user = type switch
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

        if (user == null)
            return onFailure("User not found.");

        var returnType = onSuccess.Method.ReturnType;

        if (typeof(Task).IsAssignableFrom(returnType))
        {
            var task = (Task)onSuccess.DynamicInvoke(user)!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return (T)resultProperty!.GetValue(task)!;
        }
        else
        {
            return (T)onSuccess.DynamicInvoke(user)!;
        }
    }
}