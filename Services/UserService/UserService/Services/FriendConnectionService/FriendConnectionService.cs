using Microsoft.EntityFrameworkCore;
using UserService.Models.Requests;
using UserService.Services.User;
using UserService.Models;
using UserService.Models.Responses;
using Textinger.Shared.Responses;
using UserService.Repository.AppUserRepository;

namespace UserService.Services.FriendConnectionService;

public class FriendConnectionService(
    MainDatabaseContext context,
    IApplicationUserService userServices,
    IUserRepository userRepository
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
        
        if (friendName == existingUserNameDto.UserName)
        {
            return new FailureWithMessage("You cannot send friend request to yourself.");
        }
        
        if (await AlreadySentFriendRequest(new FriendRequest(userId.ToString(), existingNewFriendIdDto.Id.ToString())))
        {
            return new FailureWithMessage("You already sent a friend request to this user!");
        }
        
        var friendRequest = new FriendConnection(userId, existingNewFriendIdDto.Id);

        var savedRequest = await context.FriendConnections.AddAsync(friendRequest);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Failed to save changes.");
        }

        return new SuccessWithDto<ShowFriendRequestDto>(new ShowFriendRequestDto(
            savedRequest.Entity.ConnectionId,
            existingUserNameDto.UserName,
            userId,
            savedRequest.Entity.SentTime,
            friendName,
            existingNewFriendIdDto.Id));
    }

    public async Task<ResponseBase> GetAllPendingRequestsAsync(Guid userId)
    {
        var rawFriendConnection = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Received = u.ReceivedFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new
                    {
                        rfr.ConnectionId,
                        SenderId = rfr.Sender!.Id,
                        SenderUserName = rfr.Sender!.UserName!,
                        rfr.SentTime,
                        ReceiverUserName = rfr.Receiver!.UserName!,
                        ReceiverId = rfr.Receiver.Id
                    }),
                Sent = u.SentFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new
                    {
                        rfr.ConnectionId,
                        SenderId = rfr.Sender!.Id,
                        SenderUserName = rfr.Sender!.UserName!,
                        rfr.SentTime,
                        ReceiverUserName = rfr.Receiver!.UserName!,
                        ReceiverId = rfr.Receiver.Id
                    })
            })
            .FirstOrDefaultAsync();
        
        if (rawFriendConnection == null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var friendRequestsDto = rawFriendConnection.Received
            .Concat(rawFriendConnection.Sent)
            .Select(rfr => new ShowFriendRequestDto(
                rfr.ConnectionId,
                rfr.SenderUserName,
                rfr.SenderId,
                rfr.SentTime,
                rfr.ReceiverUserName,
                rfr.ReceiverId
            ))
            .ToList();
        

        return new SuccessWithDto<List<ShowFriendRequestDto>>(friendRequestsDto);
    }
    
    public async Task<ResponseBase> GetPendingRequestCountAsync(Guid userId)
    {
        var pendingRequestCount = await context.FriendConnections
            .Where(rfr => rfr.ReceiverId == userId && rfr.Status == FriendStatus.Pending)
            .CountAsync();
        
        return new SuccessWithDto<NumberDto>(new NumberDto(pendingRequestCount));
    }

    private async Task<bool> AlreadySentFriendRequest(FriendRequest request)
    {
        if (!Guid.TryParse(request.Receiver, out var receiverGuid) || !Guid.TryParse(request.SenderId, out var senderGuid))
        {
            throw new ArgumentException("Invalid Receiver or SenderId format.");
        }

        return await context.Users
            .AnyAsync(u => u.ReceivedFriendRequests
                .Any(fc => fc.ReceiverId == receiverGuid && fc.SenderId == senderGuid));
    }

    public async Task<ResponseBase> AcceptReceivedFriendRequestAsync(Guid userId, Guid requestId)
    {
        var existingRequest = await context.FriendConnections!.FindAsync(requestId);
        if (existingRequest == null)
        {
            return new FailureWithMessage("Request not found.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        existingRequest.SetStatusToAccepted();

        var linkResult = await LinkUsersAsFriendsAsync(userId, existingRequest.SenderId);
        if (linkResult is Failure)
        {
            await transaction.RollbackAsync();
            return linkResult;
        }

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailureWithMessage("Failed to persist friend request acceptance.");
        }

        await transaction.CommitAsync();
        return new Success();
    }

    private async Task<ResponseBase> LinkUsersAsFriendsAsync(Guid receiverId, Guid senderId)
    {
        var user = await context.Users
            .Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == receiverId);
        
        var friend = await context.Users
            .Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == senderId);

        if (user == null || friend == null)
        {
            return new FailureWithMessage("User or sender not found.");
        }

        user.Friends.Add(friend);
        friend.Friends.Add(user);

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

        context.FriendConnections!.Remove(request);

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Failed to update connections.");
        }

        return new Success();
    }

    public async Task<ResponseBase> GetFriendsAsync(Guid userId)
    {
        var userWithFriends = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Friends = context.FriendConnections
                    .Where(fr => (fr.ReceiverId == u.Id || fr.SenderId == u.Id) && fr.Status == FriendStatus.Accepted)
                    .Select(fr => new ShowFriendRequestDto(
                        fr.ConnectionId,
                        fr.Receiver!.UserName!,
                        fr.ReceiverId,
                        fr.AcceptedTime,
                        fr.Sender!.UserName!,
                        fr.SenderId))
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (userWithFriends is null)
        {
            return new FailureWithMessage("User not found.");
        }

        return new SuccessWithDto<IList<ShowFriendRequestDto>>(userWithFriends.Friends);
    }

    public async Task<ResponseBase> GetConnectionIdAsync(Guid userId, Guid friendId)
    {
        var connectionDto = await context.FriendConnections
            .Where(fc => fc.ReceiverId == userId && fc.SenderId == friendId ||  fc.ReceiverId == friendId && fc.SenderId == userId)
            .Select(fc => new ConnectionIdAndAcceptedTimeDto(fc.ConnectionId, fc.AcceptedTime!.Value))
            .SingleOrDefaultAsync();

        if (connectionDto is null)
        {
            return new FailureWithMessage("Connection not found.");
        }

        return new SuccessWithDto<ConnectionIdAndAcceptedTimeDto>(connectionDto);
    }

    public async Task<ResponseBase> DeleteFriendAsync(Guid userId, Guid connectionId)
    {
        var connection = await context.FriendConnections
            .Include(fc => fc.Sender)
                .ThenInclude(u => u.Friends)
            .Include(fc => fc.Receiver)
                .ThenInclude(u => u.Friends)
            .FirstOrDefaultAsync(fc => fc.ConnectionId == connectionId);
        
        if (connection == null)
        {
            return new FailureWithMessage("Cannot find friend connection.");
        }
        
        await context.Entry(connection.Sender)
            .Collection(u => u.Friends)
            .Query()
            .Where(f => f.Id == connection.ReceiverId)
            .AsSingleQuery()
            .LoadAsync();
        
        await context.Entry(connection.Receiver)
            .Collection(u => u.Friends)
            .Query()
            .Where(f => f.Id == connection.SenderId)
            .AsSingleQuery()
            .LoadAsync();

       if (userId != connection.Sender.Id && userId != connection.Receiver.Id)
       {
           return new FailureWithMessage("You don't have permission for deletion.");
       }

       await using var transaction = await context.Database.BeginTransactionAsync();

        context.FriendConnections.Remove(connection);

        var unlinkResult = UnlinkFriendsAsync(connection.Sender, connection.Receiver);
        if (unlinkResult is FailureWithMessage)
        {
            await transaction.RollbackAsync();
            return unlinkResult;
        }

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailureWithMessage("Failed to update connections.");
        }

        await transaction.CommitAsync();
        return new Success();
    }

    private ResponseBase UnlinkFriendsAsync(ApplicationUser sender, ApplicationUser receiver)
    {
        sender.Friends.Remove(receiver);
        receiver.Friends.Remove(sender);

        return new Success();
    }
}