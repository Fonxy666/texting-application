using UserService.Models;

namespace UserService.Repository.KeyRepository;

public interface IKeyRepository
{
    Task<bool> AddKeyAsync(EncryptedSymmetricKey key);
    void AttachNewKey(EncryptedSymmetricKey key);
}