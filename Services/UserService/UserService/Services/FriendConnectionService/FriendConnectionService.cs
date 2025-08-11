using System.Linq.Expressions;
using UserService.Models.Requests;
using UserService.Services.User;
using UserService.Models;
using UserService.Models.Responses;
using Textinger.Shared.Responses;
using UserService.Repository.AppUserRepository;
using UserService.Repository.BaseDbRepository;
using UserService.Repository.FConnectionRepository;

namespace UserService.Services.FriendConnectionService;

public class FriendConnectionService(
    IApplicationUserService userServices,
    IUserRepository userRepository,
    IFriendConnectionRepository friendRepository,
    IBaseDatabaseRepository baseRepository
    ) : IFriendConnectionService
{
    public async Task<ResponseBase> SendFriendRequestAsync(Guid userId, string friendName)
    {
        var existingUserNameDto = await userRepository.GetUsernameDtoAsync(userId);
        var existingNewFriendIdDto = await userRepository.GetUserIdDtoAsync(friendName);
        
        if (existingNewFriendIdDto is null)
        {
            return new FailureWithMessage($"There is no User with this username: {friendName}");
        }
        
        if (friendName == existingUserNameDto!.UserName)
        {
            return new FailureWithMessage("You cannot send friend request to yourself.");
        }
        
        if (await AlreadySentFriendRequest(new FriendRequest(userId.ToString(), existingNewFriendIdDto.Id.ToString())))
        {
            return new FailureWithMessage("You already sent a friend request to this user!");
        }
        
        var friendRequest = new FriendConnection(userId, existingNewFriendIdDto.Id);

        var saveResult = await friendRepository.AddFriendConnectionAsync(friendRequest);
        if (saveResult is null)
        {
            return new FailureWithMessage("Failed to save changes.");
        }

        return new SuccessWithDto<ShowFriendRequestDto>(new ShowFriendRequestDto(
            saveResult.ConnectionId,
            existingUserNameDto.UserName,
            userId,
            saveResult.SentTime,
            friendName,
            existingNewFriendIdDto.Id));
    }

    public async Task<ResponseBase> GetAllPendingRequestsAsync(Guid userId)
    {
        var rawFriendConnection = await friendRepository.GetAllPendingRequestsAsync(userId);
        
        if (rawFriendConnection == null)
        {
            return new FailureWithMessage("User not found.");
        }

        var friendRequestsDto = rawFriendConnection.Received
            .Concat(rawFriendConnection.Sent)
            .ToList();
        

        return new SuccessWithDto<List<ShowFriendRequestDto>>(friendRequestsDto);
    }
    
    public async Task<ResponseBase> GetPendingRequestCountAsync(Guid userId)
    {
        var pendingRequestCount = await friendRepository.GetPendingRequestsCountAsync(userId);
        
        return new SuccessWithDto<NumberDto>(new NumberDto(pendingRequestCount));
    }

    private async Task<bool> AlreadySentFriendRequest(FriendRequest request)
    {
        if (!Guid.TryParse(request.Receiver, out var receiverGuid) || !Guid.TryParse(request.SenderId, out var senderGuid))
        {
            throw new ArgumentException("Invalid Receiver or SenderId format.");
        }

        return await friendRepository.IsFriendRequestAlreadySentAsync(receiverGuid, senderGuid);
    }

    public async Task<ResponseBase> AcceptReceivedFriendRequestAsync(Guid userId, Guid requestId)
    {
        var existingRequest = await friendRepository.GetFriendConnectionAsync(requestId);
        if (existingRequest is null)
        {
            return new FailureWithMessage("Request not found.");
        }

        return await baseRepository.ExecuteInTransactionAsync(async () =>
        {
            existingRequest.SetStatusToAccepted();

            var linkResult = await LinkUsersAsFriendsAsync(userId, existingRequest.SenderId);
            if (linkResult is Failure)
            {
                return linkResult;
            }
            return new Success();
        });
    }

    private async Task<ResponseBase> LinkUsersAsFriendsAsync(Guid receiverId, Guid senderId)
    {
        Expression<Func<ApplicationUser, object>> expression = u => u.Friends;
        var user = await userRepository.GetUserWithIncludeAsync(receiverId, expression);
        var friend = await userRepository.GetUserWithIncludeAsync(senderId, expression);

        if (user == null || friend == null)
        {
            return new FailureWithMessage("User or sender not found.");
        }

        var firstSync = await userRepository.AddFriendAsync(user, friend);
        var secondSync = await userRepository.AddFriendAsync(friend, user);
        if (!firstSync || !secondSync)
        {
            return  new FailureWithMessage("Database error.");
        }

        return new Success();
    }

    public async Task<ResponseBase> DeleteFriendRequestAsync(Guid userId, UserType userType, Guid requestId)
    {
        Func<ApplicationUser, IEnumerable<FriendConnection>> getRequests;
        Func<Guid, Task<ResponseBase>> getUserWithRequests;

        switch (userType)
        {
            case UserType.Receiver:
                getRequests = user => user.ReceivedFriendRequests;
                getUserWithRequests = id => userServices.GetUserWithFriendRequestsAsync(id, u => u.ReceivedFriendRequests);
                break;

            case UserType.Sender:
                getRequests = user => user.SentFriendRequests;
                getUserWithRequests = id => userServices.GetUserWithFriendRequestsAsync(id, u => u.SentFriendRequests);
                break;

            default:
                return new FailureWithMessage("Invalid user type.");
        }

        return await DeleteFriendRequestInternalAsync(userId, requestId, getUserWithRequests, getRequests);
    }

    private async Task<ResponseBase> DeleteFriendRequestInternalAsync(
        Guid userId,
        Guid requestId,
        Func<Guid, Task<ResponseBase>> getUserWithRequests,
        Func<ApplicationUser, IEnumerable<FriendConnection>> getRequests)
    {
        var result = await getUserWithRequests(userId);

        if (result is not SuccessWithDto<ApplicationUser> success || success.Data is null)
        {
            return new FailureWithMessage("User not found.");
        }

        var request = getRequests(success.Data).FirstOrDefault(fc => fc.ConnectionId == requestId);

        if (request == null)
        {
            return new FailureWithMessage("Cannot find the request.");
        }

        var deleteResult = await friendRepository.RemoveFriendConnectionAsync(request);
        if (!deleteResult)
        {
            return new FailureWithMessage("Failed to update connections.");
        }

        return new Success();
    }

    public async Task<ResponseBase> GetFriendsAsync(Guid userId)
    {
        var userFriends = await friendRepository.GetFriendsAsync(userId);

        if (userFriends is null)
        {
            return new FailureWithMessage("User not found.");
        }

        return new SuccessWithDto<IList<ShowFriendRequestDto>>(userFriends);
    }

    public async Task<ResponseBase> GetConnectionIdAsync(Guid userId, Guid friendId)
    {
        var connectionDto = await friendRepository.GetConnectionIdAndAcceptedTimeAsync(userId, friendId);

        if (connectionDto is null)
        {
            return new FailureWithMessage("Connection not found.");
        }

        return new SuccessWithDto<ConnectionIdAndAcceptedTimeDto>(connectionDto);
    }

    public async Task<ResponseBase> DeleteFriendAsync(Guid userId, Guid connectionId)
    {
        var connection = await friendRepository.GetFriendConnectionWithSenderAndReceiverAsync(userId, connectionId);
        
        if (connection == null)
        {
            return new FailureWithMessage("Cannot find friend connection.");
        }

        if (userId != connection.Sender!.Id && userId != connection.Receiver!.Id)
        {
            return new FailureWithMessage("You don't have permission for deletion.");
        }

        return await baseRepository.ExecuteInTransactionAsync<ResponseBase>(async () =>
        {
            var deleteResult = await friendRepository.RemoveFriendConnectionAsync(connection);
            if (!deleteResult)
            {
                return new FailureWithMessage("Failed to delete the friend connection.");
            }

            var unlinkResult = UnlinkFriendsAsync(connection.Sender, connection.Receiver!);
            if (unlinkResult is FailureWithMessage)
            {
                return unlinkResult;
            }
            
            return new Success();
        });
    }

    private ResponseBase UnlinkFriendsAsync(ApplicationUser sender, ApplicationUser receiver)
    {
        sender.Friends.Remove(receiver);
        receiver.Friends.Remove(sender);

        return new Success();
    }
}