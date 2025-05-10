using Microsoft.EntityFrameworkCore;
using UserService.Models.Requests;
using UserService.Services.User;
using UserService.Models;
using UserService.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Textinger.Shared.Responses;
using UserService.Helpers;

namespace UserService.Services.FriendConnectionService;

public class FriendConnectionService(
    MainDatabaseContext context,
    IApplicationUserService userServices,
    IUserHelper userHelper) : IFriendConnectionService
{
    public async Task<ResponseBase> SendFriendRequestAsync(string userId, string friendName)
    {
        async Task<ResponseBase> HandleCurrentUser(ApplicationUser existingUser)
        {
            if (existingUser.UserName == friendName)
                return new FailureWithMessage("You cannot send friend request to yourself.");

            return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
                UserIdentifierType.Username,
                friendName,
                HandleNewFriend,
                _ => new FailureWithMessage("New friend not found."));
        }

        async Task<ResponseBase> HandleNewFriend(ApplicationUser existingNewFriend)
        {
            if (await AlreadySentFriendRequest(new FriendRequest(userId, existingNewFriend.Id.ToString())))
            {
                return new FailureWithMessage("You already sent a friend request to this user!");
            }

            var friendRequest = new FriendConnection(new Guid(userId), existingNewFriend.Id);

            var savedRequest = await context.FriendConnections!.AddAsync(friendRequest);
            var affectedRows = await context.SaveChangesAsync();
            if (affectedRows == 0)
            {
                return new FailureWithMessage("Failed to save changes.");
            }

            return new SuccessWithDto<ShowFriendRequestDto>(new ShowFriendRequestDto(
                savedRequest.Entity.ConnectionId,
                savedRequest.Entity.Sender!.UserName!,
                savedRequest.Entity.SenderId.ToString(),
                savedRequest.Entity.SentTime,
                savedRequest.Entity.Receiver!.UserName!,
                savedRequest.Entity.Receiver.Id.ToString()));
        }

        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserId,
            userId,
            HandleCurrentUser,
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> GetAllPendingRequestsAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserIdIncludeReceiverAndSentRequests,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser =>
            {
                var receivedFriendRequests = await GetPendingReceivedFriendRequests(existingUser);
                if (receivedFriendRequests is FailureWithMessage)
                {
                    return receivedFriendRequests;
                }
                var sentFriendRequests = await GetPendingSentFriendRequests(existingUser);
                if (sentFriendRequests is FailureWithMessage)
                {
                    return sentFriendRequests;
                }

                var receivedList = (receivedFriendRequests as SuccessWithDto<List<ShowFriendRequestDto>>)?.Data ?? new();
                var sentList = (sentFriendRequests as SuccessWithDto<List<ShowFriendRequestDto>>)?.Data ?? new();

                var allRequests = receivedList!.Concat(sentList!).ToList();

                return new SuccessWithDto<List<ShowFriendRequestDto>>(allRequests);
            }
            ),
            message => new FailureWithMessage(message));
    }

    private Task<ResponseBase> GetPendingReceivedFriendRequests(ApplicationUser user)
    {
        var pendingRequests = user.ReceivedFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestDto(
                fr.ConnectionId,
                fr.Sender!.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver!.UserName!,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? [];
                    
        return Task.FromResult<ResponseBase>(new SuccessWithDto<List<ShowFriendRequestDto>>(pendingRequests));
    }

    private Task<ResponseBase> GetPendingSentFriendRequests(ApplicationUser user)
    {
        var pendingRequests = user.ReceivedFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestDto(
                fr.ConnectionId,
                fr.Sender!.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver!.UserName!,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? [];
                    
        return Task.FromResult<ResponseBase>(new SuccessWithDto<List<ShowFriendRequestDto>>(pendingRequests));
    }
    
    public async Task<ResponseBase> GetPendingRequestCountAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserIdIncludeReceivedRequests,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser =>
                {
                    var pendingRequestsCount = existingUser.ReceivedFriendRequests
                        .Count(fc => fc.Status == FriendStatus.Pending);

                    return new SuccessWithDto<NumberDto>(new NumberDto(pendingRequestsCount));
                }
            ),
            message => new FailureWithMessage(message));
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

    public async Task<ResponseBase> AcceptReceivedFriendRequestAsync(string userId, string requestId)
    {
        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser =>
                {
                    if (!Guid.TryParse(requestId, out var requestGuid))
                    {
                        return new FailureWithMessage("Invalid request ID format.");
                    }

                    var existingRequest = await context.FriendConnections!.FindAsync(requestGuid);
                    if (existingRequest == null)
                    {
                        return new FailureWithMessage("Request not found.");
                    }

                    await using var transaction = await context.Database.BeginTransactionAsync();

                    existingRequest.SetStatusToAccepted();

                    var linkResult = await LinkUsersAsFriendsAsync(existingUser.Id, existingRequest.SenderId);
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
            ),
            message => new FailureWithMessage(message));
    }

    private async Task<ResponseBase> LinkUsersAsFriendsAsync(Guid receiverId, Guid senderId)
    {
        var user = await context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == receiverId);
        var friend = await context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == senderId);

        if (user == null || friend == null)
        {
            return new FailureWithMessage("User or sender not found.");
        }

        user.Friends.Add(friend);
        friend.Friends.Add(user);

        return new Success();
    }

    public async Task<ResponseBase> DeleteFriendRequestAsync(string userId, string userType, string requestId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            return new FailureWithMessage("Invalid request ID format.");
        }

        return userType switch
        {
            "receiver" => await DeleteReceivedFriendRequestAsync(requestGuid, userId!),
            "sender" => await DeleteSentFriendRequestAsync(requestGuid, userId!),
            _ => new FailureWithMessage("Invalid user type.")
        };
    }

    private async Task<ResponseBase> DeleteSentFriendRequestAsync(Guid requestId, string senderId)
    {
        var user = await userServices.GetUserWithSentRequestsAsync(senderId);

        var request = (user as SuccessWithDto<ApplicationUser>)!.Data!.SentFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestId);

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

    private async Task<ResponseBase> DeleteReceivedFriendRequestAsync(Guid requestId, string receiverId)
    {
        var user = await userServices.GetUserWithReceivedRequestsAsync(receiverId);

        var request = (user as SuccessWithDto<ApplicationUser>)!.Data!.ReceivedFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestId);

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

    public async Task<ResponseBase> GetFriendsAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserIdIncludeFriends,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser =>
                {
                    var friends = new List<ShowFriendRequestDto>();

                    foreach (var fr in existingUser.Friends)
                    {
                        var connection = await GetConnectionIdAsync(existingUser.Id, fr.Id);
                        friends.Add(new ShowFriendRequestDto(
                            connection!.ConnectionId,
                            existingUser.UserName!,
                            existingUser.Id.ToString(),
                            connection.AcceptedTime,
                            fr.UserName!,
                            fr.Id.ToString()
                        ));
                    }

                    return new SuccessWithDto<List<ShowFriendRequestDto>>(friends);
                }
            ),
            message => new FailureWithMessage(message));
    }

    public async Task<FriendConnection?> GetConnectionIdAsync(Guid userId, Guid friendId)
    {
        return await context.FriendConnections!.FirstOrDefaultAsync(fc =>
            fc.ReceiverId == userId && fc.SenderId == friendId || fc.ReceiverId == friendId && fc.SenderId == userId);
    }

    public async Task<ResponseBase> DeleteFriendAsync(string userId, string connectionId)
    {
        if (!Guid.TryParse(connectionId, out var connectionGuid))
        {
            throw new ArgumentException("Invalid connectionId format.");
        }
        var userGuid = Guid.Parse(userId);

        var friendConnection = await context.FriendConnections!
            .Include(fc => fc.Sender)
            .Include(fc => fc.Receiver)
            .FirstOrDefaultAsync(fc => fc.ConnectionId == connectionGuid);

        if (friendConnection == null)
        {
            return new FailureWithMessage("Cannot find friend connection.");
        }

        if (userGuid != friendConnection.SenderId && userGuid != friendConnection.ReceiverId)
        {
            return new FailureWithMessage("You don't have permission for deletion.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        context.FriendConnections!.Remove(friendConnection);

        var unlinkResult = UnlinkFriendsAsync(friendConnection.Sender!, friendConnection.Receiver!);
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