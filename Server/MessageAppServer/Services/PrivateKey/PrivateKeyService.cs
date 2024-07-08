using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Database;

namespace Server.Services.PrivateKey;

public class PrivateKeyService(PrivateKeysDbContext context) : IPrivateKeyService
{
    private PrivateKeysDbContext Context { get; } = context;
    
    public async Task<string> GetEncryptedKeyByUserIdAsync(Guid userId)
    {
        var key = await Context.Keys!.FirstOrDefaultAsync(k => k.UserId == userId);
        return key!.EndToEndEncryptedPrivateKey;
    }

    public async Task<bool> SaveKey(Model.PrivateKey key)
    {
        try
        {
            await Context.Keys!.AddAsync(key);
            await Context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task DeleteKey(Guid userId)
    {
        await Context.Keys!.FirstOrDefaultAsync(k => k.UserId == userId);
        await Context.SaveChangesAsync();
    }
}