using ChatService.Database;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Repository.RoomRepository;

public class RoomRepository(ChatContext context) : IRoomRepository
{
    public async Task<bool> IsRoomNameTakenAsync(string roomName)
    {
        return await context.Rooms!.AnyAsync(room => room.RoomName == roomName);
    }
}