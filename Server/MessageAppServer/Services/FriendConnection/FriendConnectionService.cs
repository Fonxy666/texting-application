﻿using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.User;

namespace Server.Services.FriendConnection;

public class FriendConnectionService(MainDatabaseContext context, IUserServices userServices) : IFriendConnectionService
{
    private MainDatabaseContext Context { get; } = context;

    public async Task<Model.FriendConnection> GetFriendRequestByIdAsync(string requestId)
    {
        Console.WriteLine(requestId);
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid requestId format.");
        }
        
        return (await Context.FriendConnections!.FirstOrDefaultAsync(fc => fc.ConnectionId == requestGuid))!;
    }

    public async Task<ShowFriendRequestResponse> SendFriendRequest(FriendRequest request)
    {
        if (!Guid.TryParse(request.SenderId, out var senderGuid) || !Guid.TryParse(request.Receiver, out var receiverGuid))
        {
            throw new ArgumentException("Invalid userId format.");
        }
        
        var friendRequest = new Model.FriendConnection(senderGuid, receiverGuid);
        
        var savedRequest = await Context.FriendConnections!.AddAsync(friendRequest);
        await Context.SaveChangesAsync();

        var result = new ShowFriendRequestResponse(
            savedRequest.Entity.ConnectionId,
            savedRequest.Entity.Sender.UserName!,
            savedRequest.Entity.SenderId.ToString(),
            savedRequest.Entity.SentTime,
            savedRequest.Entity.Receiver.UserName!,
            savedRequest.Entity.Receiver.Id.ToString());

        return result;
    }

    public async Task<IEnumerable<ShowFriendRequestResponse>> GetPendingReceivedFriendRequests(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await Context.Users
            .Include(u => u.ReceivedFriendRequests)
            .ThenInclude(fr => fr.Sender)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var pendingRequests = user.ReceivedFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestResponse(
                fr.ConnectionId, 
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver.UserName!,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? new List<ShowFriendRequestResponse>();

        return pendingRequests;
    }
    
    public async Task<IEnumerable<ShowFriendRequestResponse>> GetPendingSentFriendRequests(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await Context.Users
            .Include(u => u.SentFriendRequests)
            .ThenInclude(fr => fr.Receiver)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var pendingRequests = user.SentFriendRequests?
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestResponse(
                fr.ConnectionId, 
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime,
                fr.Receiver.UserName,
                fr.Receiver.Id.ToString()
            ))
            .ToList() ?? new List<ShowFriendRequestResponse>();

        return pendingRequests;
    }
    
    public async Task<int> GetPendingRequestCount(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await Context.Users
            .Include(u => u.ReceivedFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            throw new Exception("User not found");
        }

        var pendingRequestsCount = user.ReceivedFriendRequests
            .Count(fc => fc.Status == FriendStatus.Pending);

        return pendingRequestsCount;
    }

    public async Task<bool> AlreadySentFriendRequest(FriendRequest request)
    {
        Guid receiverGuid, senderGuid;
        if (!Guid.TryParse(request.Receiver, out receiverGuid) || !Guid.TryParse(request.SenderId, out senderGuid))
        {
            throw new ArgumentException("Invalid Receiver or SenderId format.");
        }

        return await Context.Users
            .AnyAsync(u => u.ReceivedFriendRequests
                .Any(fc => fc.ReceiverId == receiverGuid && fc.SenderId == senderGuid));
    }

    public async Task<bool> AcceptReceivedFriendRequest(string requestId, string receiverId)
    {
        var requestGuid = new Guid(requestId);
        var userGuid = new Guid(receiverId);
        var request = await Context.FriendConnections.FindAsync(requestGuid);

        if (request == null || request.ReceiverId != userGuid)
        {
            throw new ArgumentException("Invalid request.");
        }

        request.SetStatusToAccepted();
        await Context.SaveChangesAsync();

        var user = await Context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == userGuid);
        var friend = await Context.Users.Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == request.SenderId);

        if (user != null && friend != null)
        {
            user.Friends.Add(friend);
            friend.Friends.Add(user);
            await Context.SaveChangesAsync();
        }
        return true;
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
            (fc.ReceiverId == userId && fc.SenderId == friendId) || (fc.ReceiverId == friendId && fc.SenderId == userId));
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