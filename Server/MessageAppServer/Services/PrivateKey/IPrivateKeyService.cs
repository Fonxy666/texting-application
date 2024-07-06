namespace Server.Services.PrivateKey;

public interface IPrivateKeyService
{
    Task<string> GetEncryptedKeyByUserIdAsync(Guid userId);
    Task SaveKey(Model.PrivateKey key);
    Task DeleteKey(Guid userId);
}