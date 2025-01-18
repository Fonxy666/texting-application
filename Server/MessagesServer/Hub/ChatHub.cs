using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MessagesServer.Database;
using MessagesServer.Model;
using MessagesServer.Model.Requests.Chat;
using MessagesServer.Model.Requests.Message;
using MessagesServer.Model.Responses.Chat;

namespace MessagesServer.Hub;

public class ChatHub(IDictionary<string, UserRoomConnection> connection, UserManager<ApplicationUser> userManager)
    : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task<string> JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        connection[Context.ConnectionId] = userConnection;
        await Clients.Group(userConnection.Room!).SendAsync("ReceiveMessage", "Textinger bot", $"{userConnection.User} has joined the room!", DateTime.Now, null, null, null, userConnection.Room);
        await SendConnectedUser(userConnection.Room!);
        return Context.ConnectionId;
    }

    public int GetConnectedUsers(string roomId)
    {
        return connection.Values.Where(user => user.Room == roomId).Select(connection => connection.User).Count();
    }

    public async Task KeyRequest(string roomId, string connectionId, string roomName)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingUser = await userManager.FindByIdAsync(userId!);
        var userConnection = connection.Values
            .Where(user => user.Room == roomId)
            .Select(user => connection.FirstOrDefault(c => c.Value == user))
            .FirstOrDefault();
        
        await Clients.Client(userConnection.Key).SendAsync("KeyRequest", new KeyRequestResponse(existingUser!.PublicKey, existingUser.Id, roomId, connectionId, roomName));
    }

    public async Task SendSymmetricKeyToRequestUser(string encryptedRoomKey, string connectionId, string roomId, string roomName)
    {
        await Clients.Client(connectionId).SendAsync("GetSymmetricKey", encryptedRoomKey, roomId, roomName);
    }

    public async Task SendMessage(MessageRequest request)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage",
                userRoomConnection.User,
                request.Message,
                DateTime.Now,
                userId,
                request.MessageId,
                new List<string>
                {
                    userId!
                },
                request.RoomId,
                request.Iv
                );
        }
    }
    
    public async Task ModifyMessage(EditMessageRequest request)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ModifyMessage", request.Id, request.Message);
        }
    }
    
    public async Task ModifyMessageSeen(MessageSeenRequest request)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ModifyMessageSeen", request.UserId);
        }
    }

    public async Task DeleteMessage(string messageId)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("DeleteMessage", messageId);
        }
    }

     public override async Task OnDisconnectedAsync(Exception? exp)
    {
        if (connection.TryGetValue(Context.ConnectionId, out UserRoomConnection roomConnection))
        {
            connection.Remove(Context.ConnectionId);
            await Clients.Group(roomConnection.Room!)
                .SendAsync("ReceiveMessage", "Textinger bot", $"{roomConnection.User} has left the room!", DateTime.Now);
            await SendConnectedUser(roomConnection.Room!);
            
            await Clients.Group(roomConnection.Room!).SendAsync("UserDisconnected", roomConnection.User);
        }

        await base.OnDisconnectedAsync(exp);
    }

    public Task SendConnectedUser(string room)
    {
        var users = connection.Values.Where(user => user.Room == room).Select(connection => connection.User);
        var newDictionary = new Dictionary<string, string>();

        foreach (var user in users)
        {
            var applicationUser = userManager.Users.FirstOrDefault(applicationUser => applicationUser.UserName == user);
            if (applicationUser != null)
            {
                newDictionary.TryAdd(user!, applicationUser.Id.ToString());
            }
        }
        return Clients.Group(room).SendAsync("ConnectedUser", newDictionary);
    }

    public async Task OnRoomDelete(string roomId)
    {
        await Clients.Group(roomId).SendAsync("RoomDeleted", roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        connection.Remove(Context.ConnectionId);
        await Clients.All.SendAsync("RoomDeleted", roomId);
    }
}