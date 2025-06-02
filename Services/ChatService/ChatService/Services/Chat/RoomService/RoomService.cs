using Microsoft.EntityFrameworkCore;
using ChatService.Model.Responses.Chat;
using ChatService.Model;
using ChatService.Database;
using ChatService.Model.Requests;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.RoomService;

public class RoomService(ChatContext context, 
    IUserGrpcService userGrpcService,
    IRoomRepository roomRepository
    ) : IRoomService
{
    public Task<bool> ExistingRoom(Guid id)
    {
        return context.Rooms!.AnyAsync(room => room.RoomId == id);
    }

    public async Task<Room?> GetRoomById(Guid roomId)
    {
        return await context.Rooms!.FirstOrDefaultAsync(room => room.RoomId == roomId);
    }
    
    public async Task<Room?> GetRoomByRoomName(string roomName)
    {
        var existingRoom = await context.Rooms!.FirstOrDefaultAsync(room => room.RoomName == roomName);

        return existingRoom;
    }

    public async Task<ResponseBase> RegisterRoomAsync(RoomRequest request, Guid userId)
    {
        var existingUser = await userGrpcService.UserExisting(userId.ToString());
        if (existingUser.Success)
        {
            return new FailureWithMessage("User not existing.");
        }
        
        var isRoomNameExisting = await roomRepository.IsRoomNameTakenAsync(request.RoomName);
        if (isRoomNameExisting)
        {
            return new FailureWithMessage("This room already exists.");
        }
        
        var room = new Room(request.RoomName, request.Password, userId);
        
        switch (request.RoomName)
        {
            case "test":
                room.SetRoomIdForTests("901d40c6-c95d-47ed-a21a-88cda341d0a9");
                break;
            case "TestRoom1":
                room.SetRoomIdForTests("801d40c6-c95d-47ed-a21a-88cda341d0a9");
                break;
        }
        
        var newRoom = await context.Rooms!.AddAsync(room);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new Failure();
        }
        
        var sendUserUpdateInfos = await userGrpcService.SendEncryptedRoomIdForUser(
            new StoreRoomKeyRequest(
                userId,
                request.EncryptedSymmetricRoomKey,
                newRoom.Entity.RoomId
            )
        );

        if (!sendUserUpdateInfos.Success)
        {
            Console.WriteLine(sendUserUpdateInfos);
            return new FailureWithMessage("There was an error communicating with the grpc server.");
        }

        var roomResponseDto = new RoomResponseDto(room.RoomId, room.RoomName);
        return new SuccessWithDto<RoomResponseDto>(roomResponseDto);
    }

    public async Task DeleteRoomAsync(Room room)
    {
        context.Rooms!.Remove(room);
        await context.SaveChangesAsync();
    }

    public async Task ChangePassword(Room room, string newPassword)
    {
        room.ChangePassword(newPassword);
        await context.SaveChangesAsync();
    }
}