using System.Collections.Concurrent;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
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
        var result = new UserResponseForWsSuccess(new ConnectionData(userId, Context.ConnectionId));
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
        var receiverId = userManager.FindByNameAsync(request.ReceiverName).Result!.Id.ToString();
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
        var receiverId = userManager.FindByNameAsync(request.ReceiverName).Result!.Id.ToString();
        
        if (Connections.TryGetValue(receiverId, out var connectionId))
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