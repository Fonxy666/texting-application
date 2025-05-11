using Microsoft.EntityFrameworkCore;
using UserService.Models;
using Textinger.Shared.Responses;

namespace UserService.Services.EncryptedSymmetricKeyService;

public class SymmetricKeyService(MainDatabaseContext context) : ISymmetricKeyService
{
    public async Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.EncryptedSymmetricKeys!.AddAsync(symmetricKey);
            await context.SaveChangesAsync();

            context.EncryptedSymmetricKeys.Attach(symmetricKey);

            var user = await context.Users.Include(u => u.UserSymmetricKeys).FirstOrDefaultAsync(u => u.Id == symmetricKey.UserId);
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
