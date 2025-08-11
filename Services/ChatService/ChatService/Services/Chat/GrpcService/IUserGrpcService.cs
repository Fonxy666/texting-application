using ChatService.Model.Requests;

namespace ChatService.Services.Chat.GrpcService;

public interface IUserGrpcService
{
    Task<BoolResponseWithMessage> UserExisting(string userId);
    Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(StoreRoomKeyRequest incomingRequest);
    Task<UserIdAndPublicKeyResponse> SendUserPublicKeyAndId(UserIdRequest request);
    Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request);
}
