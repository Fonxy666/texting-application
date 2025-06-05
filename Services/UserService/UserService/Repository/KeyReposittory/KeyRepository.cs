using UserService.Database;
using UserService.Models;

namespace UserService.Repository.KeyReposittory;

public class KeyRepository(MainDatabaseContext context) : IKeyRepository
{
    public async Task<bool> AddKeyAsync(EncryptedSymmetricKey key)
    {
        await context.EncryptedSymmetricKeys.AddAsync(key);
        return await context.SaveChangesAsync() > 0;
    }
}