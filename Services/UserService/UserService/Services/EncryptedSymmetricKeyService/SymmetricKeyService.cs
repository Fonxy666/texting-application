using System.Linq.Expressions;
using UserService.Models;
using Textinger.Shared.Responses;
using UserService.Repository.AppUserRepository;
using UserService.Repository.BaseDbRepository;
using UserService.Repository.KeyRepository;

namespace UserService.Services.EncryptedSymmetricKeyService;

public class SymmetricKeyService(IUserRepository userRepository, IKeyRepository keyRepository, IBaseDatabaseRepository baseRepository) : ISymmetricKeyService
{
    public async Task<ResponseBase> SaveNewKeyAndLinkToUserAsync(EncryptedSymmetricKey symmetricKey)
    {
        return await baseRepository.ExecuteInTransactionAsync<ResponseBase>(async () =>
        {
            var result = await keyRepository.AddKeyAsync(symmetricKey);
            if (!result)
            {
                return new FailureWithMessage("Database error.");
            }

            keyRepository.AttachNewKey(symmetricKey);

            Expression<Func<ApplicationUser, object>> navigation = u => u.UserSymmetricKeys;
            var user = await userRepository.GetUserWithIncludeAsync(symmetricKey.UserId, navigation);

            if (user == null)
            {
                return new Failure();
            }

            return new Success();
        });
    }
}
