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

    public async Task<Guid?> GetRoomCreatorIdAsync(Guid roomId)
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

    public async Task<Guid?> AddRoomAsync(Room room)
    {
        var savedEntity = await context.Rooms!.AddAsync(room);
        var result = await context.SaveChangesAsync() > 0;

        if (!result)
        {
            return null;
        }

        return savedEntity.Entity.RoomId;
    }

    public async Task<bool> DeleteRoomAsync(Room room)
    {
        context.Rooms!.Remove(room);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateRoomAsync(Room room)
    {
        context.Rooms!.Update(room);
        return await context.SaveChangesAsync() > 0;
    }
}