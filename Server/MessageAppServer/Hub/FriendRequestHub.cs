using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Model.Responses.User;

namespace Server.Hub;

public class FriendRequestHub(UserManager<ApplicationUser> userManager) : Microsoft.AspNetCore.SignalR.Hub
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

    public async Task SendFriendRequest(string senderId, string receiver)
    {
        var receiverId = userManager.Users.FirstOrDefault(au => au.UserName == receiver)!.Id;
        if (Connections.TryGetValue(receiverId.ToString(), out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveFriendRequest", senderId, receiverId);
        }
    }
}