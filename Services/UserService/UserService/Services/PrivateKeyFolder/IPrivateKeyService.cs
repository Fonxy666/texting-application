using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Services.PrivateKeyFolder;

public interface IPrivateKeyService
{
    Task<ResponseBase> GetEncryptedKeyByUserIdAsync(string userId);
    Task<ResponseBase> SaveKeyAsync(PrivateKey key, Guid userId);
    Task<ResponseBase> DeleteKey(string userId);
}