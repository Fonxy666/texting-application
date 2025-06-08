using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Services.Chat.GrpcService;
using ChatService.Model.Responses.Chat;

namespace ChatService.Hub;

public class ChatHub(IDictionary<string, UserRoomConnection> connection, IUserGrpcService userService)
    : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task<string> JoinRoom(UserRoomConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room!);
        connection[Context.ConnectionId] = userConnection;
        var response = new ReceiveMessageResponseForBot(
            "Textinger bot",
            $"{userConnection.User} has joined the room!",
            DateTime.UtcNow,
            Guid.Parse(userConnection.Room!)
        );
        await Clients.Group(userConnection.Room!)
            .SendAsync("ReceiveMessage", response);
        await SendConnectedUser(userConnection.Room!);
        return Context.ConnectionId;
    }

    public int GetConnectedUsers(string roomId)
    {
        return connection.Values.
            Where(user => user.Room == roomId)
            .Select(urc => urc.User)
            .Count();
    }
    
    public async Task KeyRequest(KeyRequest request)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier);
        var userIdAndPublicKey = await userService.SendUserPublicKeyAndId(new UserIdRequest { Id = userId } );
        var userConnection = connection.Values
            .Where(user => user.Room == request.RoomId.ToString())
            .Select(user => connection.FirstOrDefault(c => c.Value == user))
            .FirstOrDefault();

        var response = new KeyRequestResponse(
            userIdAndPublicKey!.PublicKey,
            new Guid(userIdAndPublicKey.UserId),
            request.RoomId,
            request.ConnectionId,
            request.RoomName
        );
        
        await Clients.Client(userConnection.Key).SendAsync("KeyRequest", response);
    }
    
    public async Task SendSymmetricKeyToRequestUser(GetSymmetricKeyRequest request)
    {
        var response = new SendKeyResponse(request.EncryptedRoomKey, request.RoomId, request.RoomName);
        await Clients.Client(request.ConnectionId.ToString()).SendAsync("GetSymmetricKey", response);
    }

    public async Task SendMessage(MessageRequest request)
    {
        var userId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier);
        var parsedUserId = Guid.Parse(userId!);
        var parsedMessageId = Guid.Parse(request.Message);
        
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            var response = new ReceiveMessageResponse(
                userRoomConnection.User!,
                request.Message,
                DateTime.UtcNow,
                parsedUserId,
                parsedMessageId,
                new List<Guid>
                {
                    parsedUserId
                },
                request.RoomId,
                request.Iv
            );
            await Clients.Group(userRoomConnection.Room!).SendAsync("ReceiveMessage",
                response
                );
        }
    }
    
    public async Task ModifyMessage(EditMessageRequest request)
    {
        if(connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
        {
            var response = new EditMessageResponse(request.Id, request.Message);
            await Clients.Group(userRoomConnection.Room!).SendAsync("ModifyMessage", response);
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
                .SendAsync("ReceiveMessage", "Textinger bot", $"{roomConnection.User} has left the room!", DateTime.UtcNow);
            await SendConnectedUser(roomConnection.Room!);
            
            await Clients.Group(roomConnection.Room!).SendAsync("UserDisconnected", roomConnection.User);
        }

        await base.OnDisconnectedAsync(exp);
    }

    public async Task SendConnectedUser(string room)
    {
        var users = connection.Values.Where(user => user.Room == room).Select(connection => connection.User);
        var newDictionary = new Dictionary<string, string>();

        var userNamesRequest = new UserNamesRequest();
        foreach (var user in users)
        {
            userNamesRequest.Name.Add(user);
        }
        var userIdsAndNames = await userService.SendUserNamesAndGetIds(userNamesRequest);

        foreach (var user in userIdsAndNames.Users)
        {
            newDictionary.TryAdd(user.Name!, user.Id);
        }

        await Clients.Group(room).SendAsync("ConnectedUser", newDictionary);
    }

    public async Task OnRoomDelete(string roomId)
    {
        await Clients.Group(roomId).SendAsync("RoomDeleted", roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        connection.Remove(Context.ConnectionId);
        await Clients.All.SendAsync("RoomDeleted", roomId);
    }
}