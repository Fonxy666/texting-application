using Server.Model.Requests.User;

namespace Server.Services.FriendConnection;

public interface IFriendConnectionService
{
    Task SendFriendRequest(FriendRequest request);
    Task<IEnumerable<Model.FriendConnection>> GetPendingFriendRequests(string userId);
    Task<int> GetPendingRequestCount(string userId);
    Task<bool> AlreadySentFriendRequest(FriendRequest request);
}