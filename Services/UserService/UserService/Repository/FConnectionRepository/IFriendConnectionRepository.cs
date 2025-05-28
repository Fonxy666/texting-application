using System.Linq.Expressions;
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
}