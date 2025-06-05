using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using Textinger.Shared.Responses;
using UserService.Database;
using UserService.Repository;
using UserService.Repository.AppUserRepository;
using UserService.Repository.KeyReposittory;

namespace UserService.Services.EncryptedSymmetricKeyService;

public class SymmetricKeyService(MainDatabaseContext context, IUserRepository userRepository, IKeyRepository keyRepository) : ISymmetricKeyService
{
    public async Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var result = await keyRepository.AddKeyAsync(symmetricKey);
            if (!result)
            {
                return new FailureWithMessage("Database error.");
            }

            context.EncryptedSymmetricKeys.Attach(symmetricKey);

            Expression<Func<ApplicationUser, object>> navigation = u => u.UserSymmetricKeys;
            var user = await userRepository.GetUserWithIncludeAsync(symmetricKey.UserId, navigation);
            
            if (user == null)
            {
                return new Failure();
            }

            user.UserSymmetricKeys.Add(symmetricKey);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return new Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
