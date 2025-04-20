using Microsoft.EntityFrameworkCore;
using UserService.Models.Requests;
using UserService.Services.User;
using UserService.Models;
using UserService.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Azure.Core;

namespace UserService.Services.FriendConnectionService;

public class FriendConnectionService(
    UserManager<ApplicationUser> userManager,
    MainDatabaseContext context,
    IApplicationUserService userServices) : IFriendConnectionService
{

    public async Task<FriendConnection?> GetFriendRequestByIdAsync(string requestId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid requestId format.");
        }

        var existingConnection = await context.FriendConnections!.FirstOrDefaultAsync(fc => fc.ConnectionId == requestGuid);
        if (existingConnection == null)
        {
            return null;
        }

        return existingConnection;
    }

    public async Task<ResponseBase> SendFriendRequestAsync(string userId, string friendName)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }
        else if (existingUser.UserName == friendName)
        {
            return new FailedResponseWithMessage("You cannot send friend request to yourself.");
        }

        var existingNewFriend = await userManager.FindByNameAsync(friendName);
        if (existingNewFriend == null)
        {
            return new FailedResponseWithMessage("New friend not found.");
        }

        if (await AlreadySentFriendRequest(new FriendRequest(userId!, existingNewFriend!.Id.ToString())))
        {
            return new FailedResponseWithMessage("You already sent a friend request to this user!");
        }

        var userGuid = new Guid(userId);

        var friendRequest = new FriendConnection(userGuid, existingNewFriend.Id);
        
        var savedRequest = await context.FriendConnections!.AddAsync(friendRequest);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailedResponseWithMessage("Failed to save changes.");
        }

        return new ShowFriendRequestResponseSuccess(new ShowFriendRequestData(
            savedRequest.Entity.ConnectionId,
            savedRequest.Entity.Sender!.UserName!,
            savedRequest.Entity.SenderId.ToString(),
            savedRequest.Entity.SentTime,
            savedRequest.Entity.Receiver!.UserName!,
            savedRequest.Entity.Receiver.Id.ToString()));
    }

    public async Task<ResponseBase> GetAllPendingRequestsAsync(string userId)
    {
        var receivedFriendRequests = await GetPendingReceivedFriendRequests(userId!);
        if (receivedFriendRequests is FailedResponseWithMessage)
        {
            return receivedFriendRequests;
        }
        var sentFriendRequests = await GetPendingSentFriendRequests(userId!);
        if (sentFriendRequests is FailedResponseWithMessage)
        {
            return sentFriendRequests;
        }

        var receivedList = (receivedFriendRequests as ShowFriendRequestsListResponseSuccess)!.Data;
        var sentList = (sentFriendRequests as ShowFriendRequestsListResponseSuccess)!.Data;

        var allRequests = receivedList!.Concat(sentList!).ToList();

        return new ShowFriendRequestsListResponseSuccess(allRequests);
    }

    private async Task<ResponseBase> GetPendingReceivedFriendRequests(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await context.Users
            .Include(u => u.ReceivedFriendRequests)
            .ThenInclude(fr => fr.Sender)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        var pendingRequests = user.ReceivedFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestData(
                fr.ConnectionId,
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver.UserName!,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? new List<ShowFriendRequestData>();

        return new ShowFriendRequestsListResponseSuccess(pendingRequests);
    }

    private async Task<ResponseBase> GetPendingSentFriendRequests(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await context.Users
            .Include(u => u.SentFriendRequests)
            .ThenInclude(fr => fr.Receiver)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        var pendingRequests = user.SentFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestData(
                fr.ConnectionId, 
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver.UserName,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? new List<ShowFriendRequestData>();

        return new ShowFriendRequestsListResponseSuccess(pendingRequests);
    }
    
    public async Task<ResponseBase> GetPendingRequestCountAsync(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await context.Users
            .Include(u => u.ReceivedFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        var pendingRequestsCount = user.ReceivedFriendRequests
            .Count(fc => fc.Status == FriendStatus.Pending);

        return new UserResponseSuccessWithNumber(pendingRequestsCount);
    }

    public async Task<bool> AlreadySentFriendRequest(FriendRequest request)
    {
        Guid receiverGuid, senderGuid;
        if (!Guid.TryParse(request.Receiver, out receiverGuid) || !Guid.TryParse(request.SenderId, out senderGuid))
        {
            throw new ArgumentException("Invalid Receiver or SenderId format.");
        }

        return await context.Users
            .AnyAsync(u => u.ReceivedFriendRequests
                .Any(fc => fc.ReceiverId == receiverGuid && fc.SenderId == senderGuid));
    }

    public async Task<ResponseBase> AcceptReceivedFriendRequest(string requestId, string receiverId)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            return new FailedResponseWithMessage("Invalid request ID format.");
        }

        if (!Guid.TryParse(receiverId, out var userGuid))
        {
            return new FailedResponseWithMessage("Invalid receiver ID format.");
        }

        var existingRequest = await context.FriendConnections!.FindAsync(requestGuid);
        if (existingRequest == null)
        {
            return new FailedResponseWithMessage("Request not found.");
        }

        existingRequest.SetStatusToAccepted();

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailedResponseWithMessage("Failed to update request status.");
        }

        var linkResult = await LinkUsersAsFriendsAsync(userGuid, existingRequest.SenderId);
        if (linkResult is FailedResponse)
        {
            await transaction.RollbackAsync();
            return linkResult;
        }

        await transaction.CommitAsync();
        return new UserResponseSuccess();
    }

    private async Task<ResponseBase> LinkUsersAsFriendsAsync(Guid receiverId, Guid senderId)
    {
        var user = await context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == receiverId);
        var friend = await context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == senderId);

        if (user == null || friend == null)
        {
            return new FailedResponseWithMessage("User or sender not found.");
        }

        user.Friends.Add(friend);
        friend.Friends.Add(user);

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailedResponseWithMessage("Failed to update friends.");
        }

        return new UserResponseSuccess();
    }

    public async Task<bool> DeleteSentFriendRequest(string requestId, string senderId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid requestId format.");
        }

        var user = await userServices.GetUserWithSentRequests(senderId);

        var request = user.SentFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestGuid);

        if (request == null)
        {
            return false;
        }
        
        Context.FriendConnections!.Remove(request);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteReceivedFriendRequest(string requestId, string receiverId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid requestId format.");
        }

        var user = await userServices.GetUserWithReceivedRequests(receiverId);

        var request = user.ReceivedFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestGuid);

        if (request == null)
        {
            return false;
        }
        
        Context.FriendConnections!.Remove(request);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ShowFriendRequestResponse>> GetFriends(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await Context.Users
            .Include(u => u.Friends)
            .FirstOrDefaultAsync(u => u.Id == userGuid);
    
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        await Context.SaveChangesAsync();
        var friendsResponses = new List<ShowFriendRequestResponse>();

        foreach (var fr in user.Friends)
        {
            var connection = await GetConnectionId(user.Id, fr.Id);
            friendsResponses.Add(new ShowFriendRequestResponse(
                connection.ConnectionId,
                user.UserName!,
                user.Id.ToString(),
                connection.AcceptedTime,
                fr.UserName,
                fr.Id.ToString()
            ));
        }

        return friendsResponses;
    }

    public async Task<Model.FriendConnection?> GetConnectionId(Guid userId, Guid friendId)
    {
        return await Context.FriendConnections.FirstOrDefaultAsync(fc =>
            fc.ReceiverId == userId && fc.SenderId == friendId || fc.ReceiverId == friendId && fc.SenderId == userId);
    }

    public async Task<bool> DeleteFriend(string connectionId)
    {
        if (!Guid.TryParse(connectionId, out var connectionGuid))
        {
            throw new ArgumentException("Invalid connectionId format.");
        }

        var friendConnection = await Context.FriendConnections
            .Include(fc => fc.Sender)
            .Include(fc => fc.Receiver)
            .FirstOrDefaultAsync(fc => fc.ConnectionId == connectionGuid);

        if (friendConnection == null)
        {
            return false;
        }

        Context.FriendConnections.Remove(friendConnection);
    
        var sender = await Context.Users
            .Include(u => u.Friends)
            .FirstOrDefaultAsync(u => u.Id == friendConnection.SenderId);
    
        var receiver = await Context.Users
            .Include(u => u.Friends)
            .FirstOrDefaultAsync(u => u.Id == friendConnection.ReceiverId);
    
        if (sender == null || receiver == null)
        {
            return false;
        }

        sender.Friends.Remove(receiver);
        receiver.Friends.Remove(sender);

        await Context.SaveChangesAsync();

        return true;
    }
}