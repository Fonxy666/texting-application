using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Helpers;

public class UserHelper(MainDatabaseContext context, UserManager<ApplicationUser> userManager) : IUserHelper
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
        
        var props = onSuccess.Method.GetParameters()[0].ParameterType.GetProperties().Select(p => p.Name).ToArray();
        var returningType = onSuccess.Method.GetParameters()[0].ParameterType;

        var methodInfo = typeof(UserHelper)
            .GetMethod(nameof(GetUserByProperties), BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(returningType);

        object? dto = null;
        
        switch (type)
        {
            case UserIdentifierType.Username:
                dto = await InvokeUserQueryAsync(nameof(ApplicationUser.UserName), userCredential, methodInfo!, props, returningType);
                break;

            case UserIdentifierType.UserEmail:
                dto = await InvokeUserQueryAsync(nameof(ApplicationUser.Email), userCredential, methodInfo!, props, returningType);
                break;

            case UserIdentifierType.UserId:
                dto = await InvokeUserQueryAsync(nameof(ApplicationUser.Id), userGuid!.Value, methodInfo!, props, returningType);
                break;

            case UserIdentifierType.UsernameExamineSymmetricKeys:
                await GetUserKey(userCredential, roomId!.Value);
                break;

            case UserIdentifierType.UserIdIncludeReceivedRequests:
                break;
                
            case UserIdentifierType.UserIdIncludeReceivedRequestsAndSenders:
                break;
            
            case UserIdentifierType.UserIdIncludeReceiverAndSentRequests:
                break;

            case UserIdentifierType.UserIdIncludeSentRequests:
                break;

            case UserIdentifierType.UserIdIncludeFriends:
                break;

            case UserIdentifierType.UserIdIncludeSentRequestsAndReceivers:
                break;

            case UserIdentifierType.UserIdIncludeSymmetricKeys:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported identifier type: {type}");
        }

        if (dto == null || dto.Equals(false))
        {
            return onFailure("User not found.");
        }

        var successDtoType = typeof(SuccessWithDto<>).MakeGenericType(dto.GetType());
        var successDto = Activator.CreateInstance(successDtoType, dto)!;

        return (T)successDto;
    }

    private async Task<object?> InvokeUserQueryAsync(string propertyName, object value, MethodInfo methodInfo, string[] props, Type returningType)
    {
        var parameter = Expression.Parameter(typeof(ApplicationUser), "u");
        var property = Expression.Property(parameter, propertyName);
        var constant = Expression.Constant(value, property.Type);
        var equal = Expression.Equal(property, constant);
        var predicate = Expression.Lambda(equal, parameter);
        
        var task = (Task)methodInfo?.Invoke(this, new object[] { predicate, props, returningType })!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
    
    private async Task<T?> GetUserByProperties<T>(Expression<Func<ApplicationUser, bool>> predicate, string[] propertyNames, Type returningType)
    {
        if (returningType == typeof(ApplicationUser))
        {
            return (T?)(object?)await userManager.Users.FirstOrDefaultAsync(predicate);
        }
        
        var parameter = Expression.Parameter(typeof(ApplicationUser), "u");

        var propertyAccessors = propertyNames.Select<string, Expression>(name => Expression.Property(parameter, name)).ToArray();

        var ctorArgTypes = propertyNames.Select(name => typeof(T).GetProperty(name)?.PropertyType ?? typeof(object)).ToArray();
        var ctor = returningType.GetConstructor(ctorArgTypes);

        var newDto = Expression.New(ctor!, propertyAccessors);

        var lambda = Expression.Lambda<Func<ApplicationUser, T>>(newDto, parameter);
        
        var result = await context.Users
            .Where(predicate)
            .Select(lambda)
            .SingleOrDefaultAsync();
        
        return result;
    }

    private async Task<bool?> GetUserKey(string userName, Guid roomId)
    {
        return await userManager.Users
            .Include(u => u.UserSymmetricKeys)
            .AnyAsync(u => u.UserName == userName && u.UserSymmetricKeys.Any(k => k.RoomId == roomId));
    }
}