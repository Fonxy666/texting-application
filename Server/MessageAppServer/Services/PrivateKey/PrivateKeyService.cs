using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Database;
using Server.Model.Requests;

namespace Server.Services.PrivateKey;

public class PrivateKeyService(PrivateKeysDbContext context) : IPrivateKeyService
{
    private PrivateKeysDbContext Context { get; } = context;
    
    public async Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId)
    {
        var key = await Context.Keys!.FirstOrDefaultAsync(k => k.UserId == userId);
        return new PrivateKeyResponse(key!.EndToEndEncryptedPrivateKey, key.Iv);
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

    public async Task<bool> DeleteKey(Guid userId)
    {
        try
        {
            await Context.Keys!.FirstOrDefaultAsync(k => k.UserId == userId);
            await Context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}