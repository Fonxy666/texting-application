﻿using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Responses.Chat;

namespace Server.Services.Chat.RoomService;

public class RoomService(DatabaseContext context) : IRoomService
{
    private DatabaseContext Context { get; init; } = context;
    public Task<bool> ExistingRoom(Guid id)
    {
        return Context.Rooms!.AnyAsync(room => room.RoomId == id);
    }

    public async Task<Room?> GetRoomById(Guid roomId)
    {
        var existingRoom = await Context.Rooms!.FirstOrDefaultAsync(room => room.RoomId == roomId);

        return existingRoom;
    }
    
    public async Task<Room?> GetRoomByRoomName(string roomName)
    {
        var existingRoom = await Context.Rooms!.FirstOrDefaultAsync(room => room.RoomName == roomName);

        return existingRoom;
    }

    public async Task<RoomResponse> RegisterRoomAsync(string roomName, string password, Guid creatorId)
    {
        var room = new Room(roomName, password, creatorId);
        
        if (roomName == "test")
        {
            room.SetRoomIdForTests("901d40c6-c95d-47ed-a21a-88cda341d0a9");
        }
        
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
}