using UserService.Model;

namespace UserService.Services.EncryptedSymmetricKeyService;

public interface ISymmetricKeyService
{
    Task<bool> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey);
}
