using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Responses;
using UserService.Services.FriendConnectionService;

namespace UserService.Hub;

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
    
    public async Task GetOnlineFriends(string userId)
    {
        var onlineFriendList = new List<FriendHubFriendData>();
        var userWithFriends = await userManager.Users
            .Include(user => user.Friends)
            .FirstOrDefaultAsync(user => user.Id == new Guid(userId));

        foreach (var friend in userWithFriends.Friends)
            {
            if (Connections.TryGetValue(friend.Id.ToString(), out var receiverConnectionId))
            {
                var connection = await friendConnectionService.GetConnectionIdAsync(userWithFriends.Id, friend.Id);
                onlineFriendList.Add(new FriendHubFriendData(
                    connection!.ConnectionId.ToString(),
                    userWithFriends.UserName!,
                    userWithFriends.Id.ToString(),
                    connection.AcceptedTime.ToString()!,
                    friend.UserName!,
                    friend.Id.ToString()
                ));
            }
        }
        
        await Clients.Client(Connections[userId]).SendAsync("ReceiveOnlineFriends", onlineFriendList);
    }

    public async Task<UserResponseForWsSuccess> JoinToHub(string userId)
    {
        Connections[userId] = Context.ConnectionId;
        var result = new UserResponseForWsSuccess(userId, Context.ConnectionId);
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
    
    public async Task DeleteFriendRequest(string requestId, string senderId, string receiverId)
    {
        if (!Guid.TryParse(senderId, out var senderGuid))
        {
            throw new ArgumentException("Invalid senderId format.");
        }
        if (!Guid.TryParse(receiverId, out var receiverGuid))
        {
            throw new ArgumentException("Invalid receiverId format.");
        }

        if (Connections.TryGetValue(senderGuid.ToString(), out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("DeleteFriendRequest", requestId);
        }
        if (Connections.TryGetValue(receiverGuid.ToString(), out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("DeleteFriendRequest", requestId);
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

    public async Task SendChatRoomInvite(string roomId, string roomName, string receiverName, string senderId, string senderName, string? roomKey)
    {
        var receiverId = userManager.FindByNameAsync(receiverName).Result!.Id.ToString();
        
        if (Connections.TryGetValue(receiverId, out var connectionId))
        {
            if (roomKey != null)
            {
                Console.WriteLine("fasza");
                await Clients.Client(connectionId).SendAsync("ReceiveChatRoomInvite", roomId, roomName, receiverId, senderId, senderName, roomKey);
            }
            else
            {
                await Clients.Client(connectionId).SendAsync("ReceiveChatRoomInvite", roomId, roomName, receiverId, senderId, senderName);
            }
        }
    }
}