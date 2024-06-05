using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;

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

    public Task<IEnumerable<Model.FriendConnection>> GetPendingFriendRequests(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid userId format.");
        }
        
        return Task.FromResult(Context.Users
            .Include(u => u.ReceivedFriendRequests)
            .Include(applicationUser => applicationUser.SentFriendRequests)
            .FirstOrDefault(u => u.Id == userGuid)!
            .SentFriendRequests.Where(fc => fc.Status == FriendStatus.Pending));
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