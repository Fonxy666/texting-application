using AuthenticationServer.Model.Responses.User;

namespace AuthenticationServer.Services.PrivateKey;

public interface IPrivateKeyService
{
    Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId);
    Task<bool> SaveKey(Model.PrivateKey key);
    Task<bool> DeleteKey(Guid userId);
}