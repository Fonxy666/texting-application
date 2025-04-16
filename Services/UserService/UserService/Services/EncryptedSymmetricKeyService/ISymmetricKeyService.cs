using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Services.EncryptedSymmetricKeyService;

public interface ISymmetricKeyService
{
    Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey);
}
