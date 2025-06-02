namespace ChatService.Repository.RoomRepository;

public interface IRoomRepository
{
    Task<bool> IsRoomNameTakenAsync(string roomName);
}