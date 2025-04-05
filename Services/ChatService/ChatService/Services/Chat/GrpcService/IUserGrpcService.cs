using ChatService.Model.Requests.EncryptKey;

namespace ChatService.Services.Chat.GrpcService;

public interface IUserGrpcService
{
    public Task<BoolResponseWithMessage> UserExisting(string userId);
    public Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(StoreRoomKeyRequest incomingRequest);
}
