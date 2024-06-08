using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.User;

namespace Server.Services.FriendConnection;

public class FriendConnectionService(DatabaseContext context, IUserServices userServices) : IFriendConnectionService
{
    private DatabaseContext Context { get; } = context;

    public async Task<Model.FriendConnection> GetFriendRequestByIdAsync(string requestId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid userId format.");
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

        var result = new ShowFriendRequestResponse(savedRequest.Entity.ConnectionId, savedRequest.Entity.Sender.UserName!, savedRequest.Entity.SenderId.ToString(), savedRequest.Entity.SentTime);

        return result;
    }

    public Task<IEnumerable<ShowFriendRequestResponse>> GetPendingFriendRequests(string userId)
    {
        var userGuid = new Guid(userId);
        var user = Context.Users
            .Include(u => u.ReceivedFriendRequests)
            .ThenInclude(fr => fr.Sender)
            .FirstOrDefault(u => u.Id == userGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        Console.WriteLine(user.UserName);

        if (user.ReceivedFriendRequests == null)
        {
            throw new InvalidOperationException("ReceivedFriendRequests collection is null.");
        }

        var pendingRequests = user.ReceivedFriendRequests
            .Where(fr => fr.Status == FriendStatus.Pending)
            .Select(fr => new ShowFriendRequestResponse(
                fr.ConnectionId, 
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime))
            .ToList();

        return Task.FromResult<IEnumerable<ShowFriendRequestResponse>>(pendingRequests);
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
                .Any(fc => fc.ReceiverId == receiverGuid && fc.SenderId == senderGuid && fc.Status != FriendStatus.Declined));
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

        var user = userServices.GetUserWithSentRequests(senderId).Result;

        var request = user.SentFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestGuid);

        if (request == null)
        {
            return false;
        }
        
        request.SetStatusToDeclined();
        
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeclineReceivedFriendRequest(string requestId, string receiverId)
    {
        if (!Guid.TryParse(requestId, out var requestGuid))
        {
            throw new ArgumentException("Invalid requestId format.");
        }

        var user = userServices.GetUserWithReceivedRequests(receiverId).Result;

        var request = user.ReceivedFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestGuid);

        if (request == null)
        {
            return false;
        }
        
        request.SetStatusToDeclined();
        
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ShowFriendRequestResponse>> GetFriends(string userId)
    {
        var userGuid = new Guid(userId);
        var user = await Context.Users
            .Include(u => u.Friends)
            .Include(u => u.ReceivedFriendRequests)
            .ThenInclude(fr => fr.Sender)
            .Include(u => u.SentFriendRequests)
            .ThenInclude(fr => fr.Receiver)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var acceptedReceivedRequests = user.ReceivedFriendRequests
            .Where(fr => fr.Status == FriendStatus.Accepted)
            .Select(fr => new ShowFriendRequestResponse(
                fr.ConnectionId, 
                fr.Sender.UserName!,
                fr.Sender.Id.ToString(),
                fr.SentTime))
            .ToList();

        var acceptedSentRequests = user.SentFriendRequests
            .Where(fr => fr.Status == FriendStatus.Accepted)
            .Select(fr => new ShowFriendRequestResponse(
                fr.ConnectionId, 
                fr.Receiver.UserName!,
                fr.Receiver.Id.ToString(),
                fr.SentTime))
            .ToList();

        var allFriends = acceptedReceivedRequests.Concat(acceptedSentRequests).ToList();

        return allFriends;
    }
}