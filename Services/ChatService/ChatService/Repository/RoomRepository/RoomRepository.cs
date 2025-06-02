using ChatService.Database;
using ChatService.Model;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Repository.RoomRepository;

public class RoomRepository(ChatContext context) : IRoomRepository
{
    public async Task<bool> IsRoomExistingAsync(string roomName)
    {
        return await context.Rooms!.AnyAsync(room => room.RoomName == roomName);
    }

    public async Task<bool> IsRoomExistingAsync(Guid roomId)
    {
        return await context.Rooms!.AnyAsync(room => room.RoomId == roomId);
    }

    public async Task<Guid?> GetRoomCreatorId(Guid roomId)
    {
        return await context.Rooms!
            .Where(r => r.RoomId == roomId)
            .Select(r => r.CreatorId)
            .FirstOrDefaultAsync();
    }

    public async Task<Room?> GetRoomAsync(Guid roomId)
    {
        return await context.Rooms!.FirstOrDefaultAsync(r => r.RoomId == roomId);
    }

    public async Task<Room?> GetRoomAsync(string roomName)
    {
        return await context.Rooms!.FirstOrDefaultAsync(r => r.RoomName == roomName);
    }
}