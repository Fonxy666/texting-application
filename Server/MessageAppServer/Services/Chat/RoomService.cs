using Server.Contracts;
using Server.Database;
using Server.Model.Chat;
using Sprache;

namespace Server.Services.Chat;

public class RoomService(RoomsContext context) : IRoomService
{
    private RoomsContext _context { get; set; } = context;
    public async Task<CreateRoomResponse> RegisterRoomAsync(string roomName, string password)
    {
        try
        {
            var room = new Room(roomName, password);
            Console.WriteLine(room.RoomName);
            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();
            
            return new CreateRoomResponse(true, room.RoomId, room.RoomName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new CreateRoomResponse(false, "", "");
        }
    }
    

    public Task<CreateRoomResponse> LoginRoomAsync(string roomName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<RoomNameTakenResponse> RoomNameTaken(string roomName)
    {
        var isTaken = context.Rooms.Any(room => room.RoomName == roomName);
        var result = new RoomNameTakenResponse(isTaken);
        return Task.FromResult(result);
    }
}