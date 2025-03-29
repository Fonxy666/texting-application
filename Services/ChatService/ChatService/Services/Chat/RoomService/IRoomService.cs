using ChatService.Model;
using ChatService.Model.Responses.Chat;

namespace ChatService.Services.Chat.RoomService;

public interface IRoomService
{
    Task<bool> ExistingRoom(Guid id);
    Task<Room?> GetRoomById(Guid roomId);
    Task<Room?> GetRoomByRoomName(string roomName);
    Task<RoomResponse> RegisterRoomAsync(string roomName, string password, Guid creatorId, string encryptedSymmetricKey);
    Task DeleteRoomAsync(Room room);
    Task<RoomNameTakenResponse> RoomNameTaken(string roomName);
    Task ChangePassword(Room room, string newPassword);
    Task<bool> AddNewUserKey(Guid roomId, Guid userId, string key);
}