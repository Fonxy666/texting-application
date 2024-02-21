using Microsoft.AspNetCore.SignalR;
using Server.Model;
using Server.Requests;
using Server.Services.Chat.MessageService;

namespace Server.Hub;

public class ChatHub: Microsoft.AspNetCore.SignalR.Hub
{
    private readonly IDictionary<string, UserRoomConnection> _connection;
    private readonly IMessageService _messageRepository;

    public ChatHub(IDictionary<string, UserRoomConnection> connection, IMessageService messageRepository) : base()
    {
        _connection = connection;
        _messageRepository = messageRepository;
    }
    public async Task JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        Console.WriteLine();
        _connection[Context.ConnectionId] = userConnection;
        await Clients.Group(userConnection.Room!).SendAsync("ReceiveMessage", "Textinger bot", $"{userConnection.User} has joined the room!", DateTime.Now);
        await SendConnectedUser(userConnection.Room!);
    }

    public async Task SendMessage(MessageRequest request)
    {
        if(_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage", userRoomConnection.User, request.Message, DateTime.Now);
        }
    }

    public async Task SaveMessage(MessageRequest request)
    {
        var messageRequest = new MessageRequest(request.RoomId, request.UserName, request.Message);
        await _messageRepository.SendMessage(messageRequest);
    }

     public override async Task OnDisconnectedAsync(Exception? exp)
    {
        if (_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection roomConnection))
        {
            _connection.Remove(Context.ConnectionId);
            await Clients.Group(roomConnection.Room!)
                .SendAsync("ReceiveMessage", "Textinger bot", $"{roomConnection.User} has left the room!", DateTime.Now);
            await SendConnectedUser(roomConnection.Room!);
            
            await Clients.Group(roomConnection.Room!).SendAsync("UserDisconnected", roomConnection.User);
        }

        await base.OnDisconnectedAsync(exp);
    }

    public Task SendConnectedUser(string room)
    {
        var users = _connection.Values.Where(user => user.Room == room).Select(connection => connection.User);
        return Clients.Group(room).SendAsync("ConnectedUser", users);
    }
}