using UserService.Models;

namespace UserService.Repository.KeyReposittory;

public interface IKeyRepository
{
    Task<bool> AddKeyAsync(EncryptedSymmetricKey key);
}