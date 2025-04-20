using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;

namespace UserService.Services.FriendConnectionService;

public interface IFriendConnectionService
{
    Task<FriendConnection> GetFriendRequestByIdAsync(string requestId);
    Task<ResponseBase> SendFriendRequestAsync(string userId, string friendName);
    Task<ResponseBase> GetAllPendingRequestsAsync(string userId);
    Task<ResponseBase> GetPendingRequestCountAsync(string userId);
    Task<ResponseBase> AlreadySentFriendRequest(FriendRequest request);
    Task<ResponseBase> AcceptReceivedFriendRequest(string requestId, string receiverId);
    Task<ResponseBase> DeleteSentFriendRequest(string requestId, string senderId);
    Task<ResponseBase> DeleteReceivedFriendRequest(string requestId, string receiverId);
    Task<IEnumerable<ResponseBase>> GetFriends(string userId);
    Task<FriendConnection?> GetConnectionId(Guid userId, Guid friendId);
    Task<bool> DeleteFriend(string connectionId);
}