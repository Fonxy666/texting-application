using UserService.Model;
using UserService.Model.Responses;

namespace UserService.Services.PrivateKeyFolder;

public interface IPrivateKeyService
{
    Task<ResponseBase> GetEncryptedKeyByUserIdAsync(string userId);
    Task<bool> SaveKeyAsync(PrivateKey key, Guid userId);
    Task<bool> DeleteKey(Guid userId);
}