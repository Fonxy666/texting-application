using UserService.Models;
using Textinger.Shared.Responses;

namespace UserService.Services.EncryptedSymmetricKeyService;

public interface ISymmetricKeyService
{
    Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey);
}
