using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using Textinger.Shared.Responses;
using UserService.Database;

namespace UserService.Hub;

public class FriendRequestHub(UserManager<ApplicationUser> userManager, MainDatabaseContext databaseContext) : Microsoft.AspNetCore.SignalR.Hub
{
    public static ConcurrentDictionary<string, string> Connections = new();

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = Connections.FirstOrDefault(x => x.Value == connectionId).Key;
        if (userId != null)
        {
            Connections.TryRemove(userId, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task GetOnlineFriends(string userId)
    {
        var userGuid = Guid.Parse(userId);
        var userFriends =  await databaseContext.Users
            .Where(u => u.Id == userGuid)
            .Select(u => new
            {
                FriendConnection = databaseContext.FriendConnections
                    .Where(fr => (fr.ReceiverId == u.Id || fr.SenderId == u.Id) && fr.Status == FriendStatus.Accepted)
                    .Select(fr => new ShowFriendRequestDto(
                        fr.ConnectionId,
                        fr.Receiver!.UserName!,
                        fr.ReceiverId,
                        fr.AcceptedTime,
                        fr.Sender!.UserName!,
                        fr.SenderId))
            })
            .SingleOrDefaultAsync();

        var onlineFriends = userFriends.FriendConnection.ToList()
            .Where(fc =>
                (fc.ReceiverId != userGuid && Connections.TryGetValue(fc.ReceiverId.ToString(), out var _)) ||
                (fc.SenderId != userGuid && Connections.TryGetValue(fc.SenderId.ToString(), out var _)));
        
        await Clients.Client(Connections[userId]).SendAsync("ReceiveOnlineFriends", onlineFriends);
    }

    public async Task<SuccessWithDto<UserIdAndConnectionIdDto>> JoinToHub(string userId)
    {
        Connections[userId] = Context.ConnectionId;
        var result = new SuccessWithDto<UserIdAndConnectionIdDto>(new UserIdAndConnectionIdDto(Guid.Parse(userId), Context.ConnectionId));
        return result;
    }

    public async Task SendFriendRequest(ManageFriendRequest request)
    {
        var receiverId = userManager.FindByNameAsync(request.ReceiverName).Result!.Id.ToString();
        if (Connections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveFriendRequest", request);
        }
        if (Connections.TryGetValue(request.SenderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("ReceiveFriendRequest", request);
        }
    }
    
    public async Task AcceptFriendRequest(ManageFriendRequest request)
    {
        if (Connections.TryGetValue(request.SenderId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("AcceptFriendRequest", request);
        }
    }
    
    public async Task DeleteFriendRequest(DeleteFriendRequest request)
    {
        if (!Guid.TryParse(request.SenderId, out var senderGuid))
        {
            throw new ArgumentException("Invalid senderId format.");
        }
        if (!Guid.TryParse(request.ReceiverId, out var receiverGuid))
        {
            throw new ArgumentException("Invalid receiverId format.");
        }

        if (Connections.TryGetValue(senderGuid.ToString(), out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("DeleteFriendRequest", request.RequestId);
        }
        if (Connections.TryGetValue(receiverGuid.ToString(), out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("DeleteFriendRequest", request.RequestId);
        }
    }
    
    public async Task DeleteFriend(DeleteFriendRequest request)
    {
        if (Connections.TryGetValue(request.SenderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("DeleteFriend", request.RequestId);
        }
        if (Connections.TryGetValue(request.ReceiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("DeleteFriend", request.RequestId);
        }
    }

    public async Task SendChatRoomInvite(ChatRoomInviteRequest request)
    {
        var receiverId = await databaseContext.Users
            .Where(u => u.UserName == request.ReceiverName)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        
        if (Connections.TryGetValue(receiverId.ToString(), out var connectionId))
        {
            if (request.RoomKey != null)
            {
                await Clients.Client(connectionId).SendAsync("ReceiveChatRoomInvite", request);
            }
            else
            {
                await Clients.Client(connectionId).SendAsync("ReceiveChatRoomInvite", request);
            }
        }
    }
}