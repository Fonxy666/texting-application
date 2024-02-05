namespace Server.Services.Chat;

public interface IRoomService
{
    Task<RoomResponse> RegisterRoomAsync(string roomName, string password);
    Task<RoomResponse> LoginRoomAsync(string roomName, string password);
    Task<RoomNameTakenResponse> RoomNameTaken(string roomName);
}