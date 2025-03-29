using Microsoft.EntityFrameworkCore;
using ChatService.Model.Responses.Chat;
using ChatService.Model;
using ChatService.Database;

namespace ChatService.Services.Chat.RoomService;

public class RoomService(ChatContext context) : IRoomService
{
    private ChatContext Context { get; } = context;
    public Task<bool> ExistingRoom(Guid id)
    {
        return Context.Rooms!.AnyAsync(room => room.RoomId == id);
    }

    public async Task<Room?> GetRoomById(Guid roomId)
    {
        return await Context.Rooms!.FirstOrDefaultAsync(room => room.RoomId == roomId);
    }
    
    private async Task<Room?> GetRoomByIdWithKeys(Guid roomId)
    {
        return await Context.Rooms!
            .Include(r => r.EncryptedSymmetricKeys)
            .FirstOrDefaultAsync(room => room.RoomId == roomId);
    }
    
    public async Task<Room?> GetRoomByRoomName(string roomName)
    {
        var existingRoom = await Context.Rooms!.FirstOrDefaultAsync(room => room.RoomName == roomName);

        return existingRoom;
    }

    public async Task<RoomResponse> RegisterRoomAsync(string roomName, string password, Guid creatorId, string encryptedSymmetricKey)
    {
        var room = new Room(roomName, password, creatorId);
        
        switch (roomName)
        {
            case "test":
                room.SetRoomIdForTests("901d40c6-c95d-47ed-a21a-88cda341d0a9");
                break;
            case "TestRoom1":
                room.SetRoomIdForTests("801d40c6-c95d-47ed-a21a-88cda341d0a9");
                break;
        }
        
        room.AddNewSymmetricKey(creatorId, encryptedSymmetricKey);
        
        await Context.Rooms!.AddAsync(room);
        await Context.SaveChangesAsync();
        
        return new RoomResponse(true, room.RoomId.ToString(), room.RoomName);
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

    public async Task ChangePassword(Room room, string newPassword)
    {
        room.ChangePassword(newPassword);
        await Context.SaveChangesAsync();
    }

    public async Task<bool> AddNewUserKey(Guid roomId, Guid userId, string key)
    {
        var existingRoom = await GetRoomByIdWithKeys(roomId);
        if (existingRoom == null)
        {
            return false;
        }

        var userKey = new EncryptedSymmetricKey(userId, key, roomId);

        existingRoom.EncryptedSymmetricKeys.Add(userKey);
        Context.Entry(userKey).State = EntityState.Added;

        await Context.SaveChangesAsync();
        return true;
    }
}