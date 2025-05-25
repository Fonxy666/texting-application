using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using Textinger.Shared.Responses;
using UserService.Repository;

namespace UserService.Services.EncryptedSymmetricKeyService;

public class SymmetricKeyService(MainDatabaseContext context, IUserRepository userRepository) : ISymmetricKeyService
{
    public async Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.EncryptedSymmetricKeys!.AddAsync(symmetricKey);
            await context.SaveChangesAsync();

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
