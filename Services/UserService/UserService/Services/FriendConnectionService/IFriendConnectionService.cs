using UserService.Models;
using Textinger.Shared.Responses;

namespace UserService.Services.FriendConnectionService;

public interface IFriendConnectionService
{
    Task<ResponseBase> SendFriendRequestAsync(Guid userId, string friendName);
    Task<ResponseBase> GetAllPendingRequestsAsync(Guid userId);
    Task<ResponseBase> GetPendingRequestCountAsync(Guid userId);
    Task<ResponseBase> AcceptReceivedFriendRequestAsync(Guid userId, Guid requestId);
    Task<ResponseBase> DeleteFriendRequestAsync(Guid userId, UserType userType, Guid requestId);
    Task<ResponseBase> GetFriendsAsync(Guid userId);
    Task<ResponseBase> DeleteFriendAsync(Guid userId, Guid connectionId);
    Task<ResponseBase> GetConnectionIdAsync(Guid userId, Guid friendId);
}