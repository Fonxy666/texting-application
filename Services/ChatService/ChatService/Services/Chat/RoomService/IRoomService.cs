using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Chat;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.RoomService;

public interface IRoomService
{
    Task<bool> ExistingRoom(Guid id);
    Task<Room?> GetRoomById(Guid roomId);
    Task<Room?> GetRoomByRoomName(string roomName);
    Task<ResponseBase> RegisterRoomAsync(RoomRequest request, Guid userId);
    Task DeleteRoomAsync(Room room);
    Task ChangePassword(Room room, string newPassword);
}