using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;

namespace UserService.Repository.AppUserRepository;

public class UserRepository(MainDatabaseContext context) : IUserRepository
{
    public async Task<UserNameDto?> GetUsernameDtoAsync(Guid userId)
    {
        return await context.Users
            .Where(u => u.Id ==  userId)
            .Select(u => 
                new UserNameDto(u.UserName!))
            .FirstOrDefaultAsync();
    }
    
    public async Task<UserNameDto?> GetUsernameDtoAsync(string userName)
    {
        return await context.Users
            .Where(u => u.UserName ==  userName)
            .Select(u => 
                new UserNameDto(u.UserName!))
            .FirstOrDefaultAsync();
    }

    public async Task<UserIdDto?> GetUserIdDtoAsync(string userName)
    {
        return await context.Users
            .Where(u => u.UserName ==  userName)
            .Select(u => 
                new UserIdDto(u.Id))
            .FirstOrDefaultAsync();
    }

    public async Task<UsernameUserEmailAndTwoFactorEnabledDto?> GetUsernameUserEmailAndTwoFactorEnabledAsync(Guid userId)
    {
        return await context.Users
            .Where(u => u.Id ==  userId)
            .Select(u => 
                new UsernameUserEmailAndTwoFactorEnabledDto(u.UserName!, u.Email!, u.TwoFactorEnabled))
            .FirstOrDefaultAsync();
    }

    public async Task<ApplicationUser?> GetUserWithIncludeAsync(Guid userId, Expression<Func<ApplicationUser, object>> includeNavigation)
    {
        return await context.Users
            .Include(includeNavigation)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<UserPrivateKeyDto?> GetUserPrivateKeyAsync(Guid userId, Guid roomId)
    {
        return await context.Users
            .Include(u => u.UserSymmetricKeys)
            .Where(u => u.Id == userId &&
                        u.UserSymmetricKeys.Any(esk => esk.RoomId == roomId))
            .Select(u => new UserPrivateKeyDto(
                u.UserSymmetricKeys.First(k => k.RoomId == roomId).EncryptedKey))
            .FirstOrDefaultAsync();
    }

    public async Task<UserPublicKeyDto?> GetUserPublicKeyAsync(string userName)
    {
        return await context.Users
            .Where(u => u.UserName ==  userName)
            .Select(u => new UserPublicKeyDto(u.PublicKey))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsUserHaveSymmetricKeyForRoomAsync(string userName, Guid roomId)
    {
        return await context.Users
            .Include(u => u.UserSymmetricKeys)
            .AnyAsync(u => u.UserName == userName && u.UserSymmetricKeys
                .Any(esk => esk.RoomId == roomId));
    }

    public async Task<ApplicationUser?> ValidateUserInputAsync(RegistrationRequest request)
    {
        return await context.Users
            .Where(u => u.Email == request.Email 
                        || u.UserName == request.Username 
                        || u.PhoneNumber == request.PhoneNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsUserExistingAsync(Guid userId)
    {
        return await context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<UserIdAndPublicKeyDto?> GetUserIdAndPublicKeyAsync(Guid userId)
    {
        return await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserIdAndPublicKeyDto(u.Id, u.PublicKey))
            .FirstOrDefaultAsync();
    }

    public async Task<UserIdAndUserNameDto?> GetUserIdAndUserNameAsync(string userName)
    {
        return await context.Users
            .Where(u => u.UserName == userName)
            .Select(u => new UserIdAndUserNameDto(u.Id, u.UserName!))
            .FirstOrDefaultAsync();
    }
}