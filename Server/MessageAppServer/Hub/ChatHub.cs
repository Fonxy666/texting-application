using Microsoft.AspNetCore.SignalR;
using Server.Model;

namespace Server.Hub;

public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly IDictionary<string, UserRoomConnection> _connection;

    public ChatHub(IDictionary<string, UserRoomConnection> connection)
    {
        _connection = connection;
    }
    
    public async Task JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        _connection[Context.ConnectionId] = userConnection;
        await Clients.Group(userConnection.Room!).SendAsync("ReceivedMessage", "Textinger bot", $"{userConnection.User} has joined the room!");
        await SendConnectedUser(userConnection.Room!);
    }

    public async Task SendMessage(string message)
    {
        if(_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("Received message", userRoomConnection.User, message, DateTime.Now);
        }
    }

    public Task SendConnectedUser(string room)
    {
        var users = _connection.Values.Where(user => user.Room == room).Select(connection => connection.User);
        return Clients.Group(room).SendAsync("ConnectedUser", users);
    }
}