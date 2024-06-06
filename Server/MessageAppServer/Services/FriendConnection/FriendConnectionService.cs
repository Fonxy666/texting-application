using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;

namespace Server.Services.FriendConnection;

public class FriendConnectionService(DatabaseContext context) : IFriendConnectionService
{
    private DatabaseContext Context { get; } = context;
    public async Task SendFriendRequest(FriendRequest request)
    {
        if (!Guid.TryParse(request.SenderId, out var senderGuid) || !Guid.TryParse(request.Receiver, out var receiverGuid))
        {
            throw new ArgumentException("Invalid userId format.");
        }
        
        var friendRequest = new Model.FriendConnection(senderGuid, receiverGuid);
        
        await Context.FriendConnections!.AddAsync(friendRequest);
        await Context.SaveChangesAsync();
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
}