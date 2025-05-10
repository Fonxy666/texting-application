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
                         UserIdentifierType.UserIdIncludeReceivedRequests or 
                         UserIdentifierType.UserIdIncludeSentRequests or 
                         UserIdentifierType.UserIdIncludeFriends or
                         UserIdentifierType.UserIdIncludeReceiverAndSentRequests or
                         UserIdentifierType.UserIdIncludeSymmetricKeys or
                         UserIdentifierType.UserIdIncludeReceivedRequestsAndSenders or
                         UserIdentifierType.UserIdIncludeSentRequestsAndReceivers
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
            UserIdentifierType.UserIdIncludeReceivedRequests => await userManager.Users
                .Include(u => u.ReceivedFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential)),
            UserIdentifierType.UserIdIncludeSentRequests => await userManager.Users
                .Include(u => u.SentFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == new Guid(userCredential)),
            UserIdentifierType.UserIdIncludeReceivedRequestsAndSenders => await userManager.Users
                .Include(u => u.SentFriendRequests)
                .ThenInclude(fr => fr.Receiver)
                .FirstOrDefaultAsync(u => u.Id == userGuid),
            UserIdentifierType.UserIdIncludeReceiverAndSentRequests => await userManager.Users
                .Include(u => u.SentFriendRequests)
                .Include(u => u.ReceivedFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == userGuid),
            UserIdentifierType.UserIdIncludeSentRequestsAndReceivers => await userManager.Users
                .Include(u => u.SentFriendRequests)
                .ThenInclude(fr => fr.Receiver)
                .FirstOrDefaultAsync(u => u.Id == userGuid),
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
            var result = resultProperty?.GetValue(task);
            if (result is not T typedResult)
            {
                throw new InvalidCastException($"Expected result of type {typeof(T)}, but got {result?.GetType()}");
            }
            return typedResult;
        }
        else
        {
            return (T)onSuccess.DynamicInvoke(user)!;
        }
    }
}