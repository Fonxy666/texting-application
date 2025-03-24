using AuthenticationService.Model;
using AuthenticationService.Model.Responses.User;

namespace AuthenticationService.Services.PrivateKeyService;

public interface IPrivateKeyService
{
    Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId);
    Task<bool> SaveKeyAsync(PrivateKey key, Guid userId);
    Task<bool> DeleteKey(Guid userId);
}