using ChatService.Model.Requests;
using ChatService.Services.Chat.GrpcService;

namespace ChatServiceTests;

public class FakeUserGrpcService : IUserGrpcService
{
    public Task<BoolResponseWithMessage> UserExisting(string userId)
    {
        var result = new BoolResponseWithMessage { Success = true };

        if (userId != "2f1b9e96-8c0b-4a4b-8fd3-9b4c0a447e31" && userId != "3f3b1278-5c3e-4d51-842f-14d2a6581e2e")
        {
            result = new BoolResponseWithMessage { Success = false };
        }

        return Task.FromResult(result);
    }

    public Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(StoreRoomKeyRequest incomingRequest)
    {
        return Task.FromResult(new BoolResponseWithMessage { Success = true });
    }

    public Task<UserIdAndPublicKeyResponse> SendUserPublicKeyAndId(UserIdRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request)
    {
        var response = new UsersResponse();

        foreach (var name in request.Names)
        {
            response.Users.Add(new UserIdAndName { Name = name, Id = Guid.NewGuid().ToString() });
        }

        return Task.FromResult(response);
    }
}