using Server.Model.Chat;
using Server.Model.Responses.Chat;

namespace Server.Services.Chat.RoomService;

public interface IRoomService
{
    Task<Room?> GetRoom(string roomName);
    Task<RoomResponse> RegisterRoomAsync(string roomName, string password);
    Task DeleteRoomAsync(Room room);
    Task<RoomNameTakenResponse> RoomNameTaken(string roomName);
}