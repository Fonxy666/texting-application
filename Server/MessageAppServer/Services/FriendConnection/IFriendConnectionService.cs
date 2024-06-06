using Server.Model.Requests.User;
using Server.Model.Responses.User;

namespace Server.Services.FriendConnection;

public interface IFriendConnectionService
{
    Task SendFriendRequest(FriendRequest request);
    Task<IEnumerable<ShowFriendRequestResponse>> GetPendingFriendRequests(string userId);
    Task<int> GetPendingRequestCount(string userId);
    Task<bool> AlreadySentFriendRequest(FriendRequest request);
}