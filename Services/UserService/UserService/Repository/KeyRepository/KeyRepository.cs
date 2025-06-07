using UserService.Database;
using UserService.Models;

namespace UserService.Repository.KeyRepository;

public class KeyRepository(MainDatabaseContext context) : IKeyRepository
{
    public async Task<bool> AddKeyAsync(EncryptedSymmetricKey key)
    {
        await context.EncryptedSymmetricKeys.AddAsync(key);
        return await context.SaveChangesAsync() > 0;
    }

    public void AttachNewKey(EncryptedSymmetricKey key)
    {
        context.EncryptedSymmetricKeys.Attach(key);
    }
}