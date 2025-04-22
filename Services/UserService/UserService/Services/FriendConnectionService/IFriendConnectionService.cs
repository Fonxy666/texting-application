using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Services.FriendConnectionService;

public interface IFriendConnectionService
{
    Task<FriendConnection> GetFriendRequestByIdAsync(string requestId);
    Task<ResponseBase> SendFriendRequestAsync(string userId, string friendName);
    Task<ResponseBase> GetAllPendingRequestsAsync(string userId);
    Task<ResponseBase> GetPendingRequestCountAsync(string userId);
    Task<ResponseBase> AcceptReceivedFriendRequestAsync(string userId, Guid requestId);
    Task<ResponseBase> DeleteFriendRequestAsync(string userId, string userType, string requestId);
    Task<ResponseBase> GetFriendsAsync(string userId);
    Task<ResponseBase> DeleteFriendAsync(Guid userId, string connectionId);
    Task<FriendConnection?> GetConnectionIdAsync(Guid userId, Guid friendId);
}