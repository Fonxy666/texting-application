using ChatService.Model;

namespace ChatService.Repository.RoomRepository;

public interface IRoomRepository
{
    Task<bool> IsRoomExistingAsync(string roomName);
    Task<bool> IsRoomExistingAsync(Guid roomId);
    Task<Guid?> GetRoomCreatorId(Guid roomId);
    Task<Room?> GetRoomAsync(Guid roomId);
    Task<Room?> GetRoomAsync(string roomName);
}