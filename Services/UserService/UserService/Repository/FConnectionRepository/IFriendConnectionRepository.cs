using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Repository.FConnectionRepository;

public interface IFriendConnectionRepository
{
    Task<ReceivedAndSentFriendRequestsDto?> GetAllPendingRequestsAsync(Guid userId);
    Task<int> GetPendingRequestsCountAsync(Guid userId);
    Task<bool> IsFriendRequestAlreadySentAsync(Guid receiverId, Guid senderId);
    Task<IList<ShowFriendRequestDto>?> GetFriendsAsync(Guid userId);
    Task<ConnectionIdAndAcceptedTimeDto?> GetConnectionIdAndAcceptedTimeAsync(Guid userId, Guid friendId);
    Task<FriendConnection?> GetFriendConnectionWithSenderAndReceiverAsync(Guid senderId, Guid connectionId);
    Task<ConnectionIdAndSentTimeDto?> AddFriendConnectionAsync(FriendConnection friendConnection);
    Task<FriendConnection?> GetFriendConnectionAsync(Guid connectionId);
    Task<bool> RemoveFriendConnectionAsync(FriendConnection connection);
    Task<IList<FriendConnection>> GetSentRequestsAsync(Guid userId);
    Task<IList<FriendConnection>> GetReceivedRequestsAsync(Guid userId);
    void RemoveFriendRangeWithOutSaveChangesAsync(IList<FriendConnection> friendConnections);
}