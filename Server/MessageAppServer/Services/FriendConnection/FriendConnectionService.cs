using Server.Database;
using Server.Model.Requests.User;

namespace Server.Services.FriendConnection;

public class FriendConnectionService(DatabaseContext context) : IFriendConnectionService
{
    private DatabaseContext Context { get; } = context;
    public async Task SendFriendRequest(FriendRequest request)
    {
        var senderGuidId = new Guid(request.SenderId);
        var receiverGuidId = new Guid(request.Receiver);
        var friendRequest = new Model.FriendConnection(senderGuidId, receiverGuidId);

        await Context.FriendConnections!.AddAsync(friendRequest);
        await Context.SaveChangesAsync();
    }
}