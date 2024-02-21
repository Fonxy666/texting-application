using Server.Responses;

namespace Server.Services.Chat.RoomService;

public interface IRoomService
{
    Task<RoomResponse> RegisterRoomAsync(string roomName, string password);
    Task<RoomResponse> LoginRoomAsync(string roomName, string password);
    Task<RoomResponse> DeleteRoomAsync(string roomName, string password);
    Task<RoomNameTakenResponse> RoomNameTaken(string roomName);
}