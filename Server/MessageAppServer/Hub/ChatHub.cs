using Microsoft.AspNetCore.SignalR;
using Server.Model;
using Server.Model.Requests.Message;
using Server.Services.Chat.MessageService;

namespace Server.Hub;

public class ChatHub(IDictionary<string, UserRoomConnection> connection, IMessageService messageRepository)
    : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        Console.WriteLine();
        connection[Context.ConnectionId] = userConnection;
        await Clients.Group(userConnection.Room!).SendAsync("ReceiveMessage", "Textinger bot", $"{userConnection.User} has joined the room!", DateTime.Now);
        await SendConnectedUser(userConnection.Room!);
    }

    public async Task SendMessage(MessageRequest request)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage", userRoomConnection.User, request.Message, DateTime.Now);
        }
    }

    public async Task SaveMessage(MessageRequest request)
    {
        var messageRequest = new MessageRequest(request.RoomId, request.UserName, request.Message, request.AsAnonymous);
        await messageRepository.SendMessage(messageRequest);
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
        return Clients.Group(room).SendAsync("ConnectedUser", users);
    }
}