using UserService.Models;
using Textinger.Shared.Responses;

namespace UserService.Services.FriendConnectionService;

public interface IFriendConnectionService
{
    Task<FriendConnection> GetFriendRequestByIdAsync(string requestId);
    Task<ResponseBase> SendFriendRequestAsync(string userId, string friendName);
    Task<ResponseBase> GetAllPendingRequestsAsync(string userId);
    Task<ResponseBase> GetPendingRequestCountAsync(string userId);
    Task<ResponseBase> AcceptReceivedFriendRequestAsync(string userId, string requestId);
    Task<ResponseBase> DeleteFriendRequestAsync(string userId, string userType, string requestId);
    Task<ResponseBase> GetFriendsAsync(string userId);
    Task<ResponseBase> DeleteFriendAsync(string userId, string connectionId);
    Task<FriendConnection?> GetConnectionIdAsync(Guid userId, Guid friendId);
}