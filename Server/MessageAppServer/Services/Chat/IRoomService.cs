using Server.Contracts;

namespace Server.Services.Chat;

public interface IRoomService
{
    Task<CreateRoomResponse> RegisterRoomAsync(string roomName, string password);
    Task<CreateRoomResponse> LoginRoomAsync(string roomName, string password);
    Task<RoomNameTakenResponse> RoomNameTaken(string roomName);
}