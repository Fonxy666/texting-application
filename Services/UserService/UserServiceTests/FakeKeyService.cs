using Textinger.Shared.Responses;
using UserService.Models;
using UserService.Models.Responses;
using UserService.Services.PrivateKeyFolder;

namespace UserServiceTests;

public class FakeKeyService : IPrivateKeyService
{
    public Task<ResponseBase> GetEncryptedKeyByUserIdAsync(string userId)
    {
        return Task.FromResult<ResponseBase>(
            new SuccessWithDto<KeyAndIvDto>(new KeyAndIvDto("testKey", "testIv")));
    }

    public Task<ResponseBase> SaveKeyAsync(PrivateKey key, Guid userId)
    {
        return Task.FromResult<ResponseBase>(new Success());
    }

    public Task<ResponseBase> DeleteKey(string userId)
    {
        return Task.FromResult<ResponseBase>(new Success());
    }
}