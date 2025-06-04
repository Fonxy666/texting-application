using ChatService.Model;

namespace ChatService.Repository.RoomRepository;

public interface IRoomRepository
{
    Task<bool> IsRoomExistingAsync(string roomName);
    Task<bool> IsRoomExistingAsync(Guid roomId);
    Task<Guid?> GetRoomCreatorIdAsync(Guid roomId);
    Task<Room?> GetRoomAsync(Guid roomId);
    Task<Room?> GetRoomAsync(string roomName);
    Task<Guid?> AddRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(Room room);
    Task<bool> UpdateRoomAsync(Room room);
}