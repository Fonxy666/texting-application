using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Helpers;

public class UserHelper(MainDatabaseContext context) : IUserHelper
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
        {
            return onFailure("Invalid user ID format.");
        }

        /* bif (type == UserIdentifierType.UsernameExamineSymmetricKeys)
        {
            var ok = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .AnyAsync(u => u.UserName == userCredential && u.UserSymmetricKeys.Any(k => k.RoomId == roomId));

            return ok ? (T)onSuccess.DynamicInvoke(new ApplicationUser { UserName = userCredential })! 
                      : onFailure($"There is no key or user with this Username: {userCredential}");
        } */
        
        /* var user = type switch
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
        }; */
        
        var props = onSuccess.Method.GetParameters()[0].ParameterType.GetProperties().Select(p => p.Name).ToArray();
        var returningType = onSuccess.Method.GetParameters()[0].ParameterType;

        var methodInfo = typeof(UserHelper)
            .GetMethod(nameof(GetUserByProperties), BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(returningType);
        
        var newDto = (Task)methodInfo?.Invoke(this, new object[] { userCredential, props, returningType })!;
        await newDto.ConfigureAwait(false);
        var dtoResult = newDto.GetType().GetProperty("Result");
        var dto = dtoResult?.GetValue(newDto);

        if (dto == null)
        {
            return onFailure("User not found.");
        }

        var successDtoType = typeof(SuccessWithDto<>).MakeGenericType(dto.GetType());
        var successDto = Activator.CreateInstance(successDtoType, dto)!;

        return (T)successDto;
    }
    
    private async Task<T?> GetUserByProperties<T>(string userCredential, string[] propertyNames, Type returningType)
    {
        var parameter = Expression.Parameter(typeof(ApplicationUser), "u");

        var propertyAccessors = propertyNames.Select<string, Expression>(name => Expression.Property(parameter, name)).ToArray();

        var ctorArgTypes = propertyNames.Select(name => typeof(T).GetProperty(name)?.PropertyType ?? typeof(object)).ToArray();
        var ctor = returningType.GetConstructor(ctorArgTypes);

        var newDto = Expression.New(ctor!, propertyAccessors);

        var lambda = Expression.Lambda<Func<ApplicationUser, T>>(newDto, parameter);

        var result = await context.Users
            .Where(u => u.Id == Guid.Parse(userCredential))
            .Select(lambda)
            .SingleOrDefaultAsync();
        
        return result;
    }
}