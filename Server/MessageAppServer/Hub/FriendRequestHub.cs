using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;
using Server.Services.User;

namespace Server.Hub;

public class FriendRequestHub(UserManager<ApplicationUser> userManager, IUserServices userServices) : Microsoft.AspNetCore.SignalR.Hub
{
    private static readonly ConcurrentDictionary<string, string> Connections = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext().Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            Connections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
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
            Console.WriteLine($"2. connection: {receiverConnectionId}");
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveFriendRequest", requestId, senderName, senderId, sentTime, receiverName, receiverId);
        }
        if (Connections.TryGetValue(senderId, out var senderConnectionId))
        {
            Console.WriteLine($"2. connection: {senderConnectionId}");
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
}