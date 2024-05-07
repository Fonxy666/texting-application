using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Responses.Chat;

namespace Server.Services.Chat.RoomService;

public class RoomService(DatabaseContext context) : IRoomService
{
    private DatabaseContext Context { get; set; } = context;
    public Task<bool> ExistingRoom(string id)
    {
        return Context.Rooms!.AnyAsync(room => room.RoomId == id);
    }

    public async Task<Room?> GetRoom(string roomName)
    {
        var existingRoom = await Context.Rooms!.FirstOrDefaultAsync(room => room.RoomName == roomName);

        return existingRoom;
    }

    public async Task<RoomResponse> RegisterRoomAsync(string roomName, string password)
    {
        var room = new Room(roomName, password);

        if (roomName == "test")
        {
            room.RoomId = "ea5c5adb-9807-4ad1-b6da-7650d821827a";
        }
        
        await Context.Rooms!.AddAsync(room);
        await Context.SaveChangesAsync();
        
        return new RoomResponse(true, room.RoomId, room.RoomName);
    }

    public async Task DeleteRoomAsync(Room room)
    {
        Context.Rooms!.Remove(room);
        await Context.SaveChangesAsync();
    }

    public Task<RoomNameTakenResponse> RoomNameTaken(string roomName)
    {
        var isTaken = context.Rooms!.Any(room => room.RoomName == roomName);
        var result = new RoomNameTakenResponse(isTaken);
        return Task.FromResult(result);
    }
}