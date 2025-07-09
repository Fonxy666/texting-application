using ChatService.Model.Requests;
using ChatService.Services.Chat.GrpcService;

namespace ChatServiceTests;

public class FakeUserGrpcService : IUserGrpcService
{
    public Task<BoolResponseWithMessage> UserExisting(string userId)
    {
        var result = new BoolResponseWithMessage { Success = true };
        return Task.FromResult(result);
    }

    public Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(StoreRoomKeyRequest incomingRequest)
    {
        throw new NotImplementedException();
    }

    public Task<UserIdAndPublicKeyResponse> SendUserPublicKeyAndId(UserIdRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request)
    {
        throw new NotImplementedException();
    }
}