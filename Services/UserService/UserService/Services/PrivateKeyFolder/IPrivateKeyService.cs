using UserService.Model;
using UserService.Model.Responses.User;

namespace UserService.Services.PrivateKeyFolder;

public interface IPrivateKeyService
{
    Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId);
    Task<bool> SaveKeyAsync(PrivateKey key, Guid userId);
    Task<bool> DeleteKey(Guid userId);
}