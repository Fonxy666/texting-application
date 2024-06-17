using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.FriendConnection;
using Server.Services.User;

namespace Server.Hub;

public class FriendRequestHub(UserManager<ApplicationUser> userManager, IFriendConnectionService friendConnectionService) : Microsoft.AspNetCore.SignalR.Hub
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

    public async Task<UserResponseForWs> JoinToHub(string userId)
    {
        Connections[userId] = Context.ConnectionId;
        var result = new UserResponseForWs(userId, Context.ConnectionId);
        return result;
    }

    public async Task SendFriendRequest(string requestId, string senderName, string senderId, string sentTime, string receiverName)
    {
        var receiverId = userManager.FindByNameAsync(receiverName).Result.Id.ToString();
        if (Connections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveFriendRequest", requestId, senderName, senderId, sentTime, receiverName, receiverId);
        }
        if (Connections.TryGetValue(senderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("ReceiveFriendRequest", requestId, senderName, senderId, sentTime, receiverName, receiverId);
        }
    }
    
    public async Task AcceptFriendRequest(string requestId, string senderName, string senderId, string sentTime, string receiverName)
    {
        var receiverId = userManager.FindByNameAsync(receiverName).Result.Id.ToString();
        if (Connections.TryGetValue(senderId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("AcceptFriendRequest", requestId, senderName, senderId, sentTime, receiverName, receiverId);
        }
    }
    
    public async Task DeclineFriendRequest(string requestId)
    {
        var request = friendConnectionService.GetFriendRequestByIdAsync(requestId).Result;
        
        if (Connections.TryGetValue(request.ReceiverId.ToString(), out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("DeclineFriendRequest", requestId);
        }
        if (Connections.TryGetValue(request.SenderId.ToString(), out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("DeclineFriendRequest", requestId);
        }
    }
    
    public async Task DeleteFriend(string requestId, string receiverId, string senderId)
    {
        if (Connections.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("DeleteFriend", requestId);
        }
        if (Connections.TryGetValue(senderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("DeleteFriend", requestId);
        }
    }

    public async Task SendChatRoomInvite(string roomId, string roomName, string receiverName, string senderId, string senderName)
    {
        var receiverId = userManager.FindByNameAsync(receiverName).Result.Id.ToString();
        if (Connections.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveChatRoomInvite", roomId, roomName, receiverId, senderId, senderName);
        }
    }
}