using AuthenticationService.Model.Responses.User;

namespace AuthenticationService.Services.PrivateKey;

public interface IPrivateKeyService
{
    Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId);
    Task<bool> SaveKey(Model.PrivateKey key, Guid userId);
    Task<bool> DeleteKey(Guid userId);
}