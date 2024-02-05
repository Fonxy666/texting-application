﻿using Server.Database;
using Server.Model.Chat;

namespace Server.Services.Chat.RoomService;

public class RoomService(RoomsContext context) : IRoomService
{
    private RoomsContext Context { get; set; } = context;
    public async Task<RoomResponse> RegisterRoomAsync(string roomName, string password)
    {
        try
        {
            var room = new Room(roomName, password);
            await Context.Rooms.AddAsync(room);
            await Context.SaveChangesAsync();
            
            return new RoomResponse(true, room.RoomId, room.RoomName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new RoomResponse(false, "", "");
        }
    }
    
    public async Task<RoomResponse> LoginRoomAsync(string roomName, string password)
    {
        var existingRoom = Context.Rooms.FirstOrDefault(room => room.RoomName == roomName)!;

        if (existingRoom.RoomId.Length < 1)
        {
            return new RoomResponse(false, "", "");
        }

        var passwordMatch = existingRoom.PasswordMatch(password);
        
        return !passwordMatch ? new RoomResponse(false, "", existingRoom.RoomName) : new RoomResponse(true, existingRoom.RoomId, existingRoom.RoomName);
    }

    public Task<RoomNameTakenResponse> RoomNameTaken(string roomName)
    {
        var isTaken = context.Rooms.Any(room => room.RoomName == roomName);
        var result = new RoomNameTakenResponse(isTaken);
        return Task.FromResult(result);
    }
}