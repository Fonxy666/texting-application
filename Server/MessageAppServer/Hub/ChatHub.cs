using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Server.Model;
using Server.Model.Requests.Message;
using Server.Services.Chat.MessageService;

namespace Server.Hub;

public class ChatHub(IDictionary<string, UserRoomConnection> connection, IMessageService messageRepository, UserManager<ApplicationUser> userManager)
    : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task<string> JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        connection[Context.ConnectionId] = userConnection;
        await Clients.Group(userConnection.Room!).SendAsync("ReceiveMessage", "Textinger bot", $"{userConnection.User} has joined the room!", DateTime.Now);
        await SendConnectedUser(userConnection.Room!);

        return Context.ConnectionId;
    }

    public async Task SendMessage(MessageRequest request)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage", userRoomConnection.User, request.Message, DateTime.Now, request.UserName, request.MessageId);
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
            await Clients.Group(userRoomConnection.Room!).SendAsync("ModifyMessageSeen", request.UserId, request.AsAnonymous, request.MessageId);
        }
    }

    public async Task SaveMessage(MessageRequest request)
    {
        var messageRequest = new MessageRequest(request.RoomId, request.UserName, request.Message, request.AsAnonymous);
        await messageRepository.SendMessage(messageRequest);
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
            newDictionary.TryAdd(user!, userManager.Users.FirstOrDefault(applicationUser => applicationUser.UserName == user)!.Id);
        }
        return Clients.Group(room).SendAsync("ConnectedUser", newDictionary);
    }
}