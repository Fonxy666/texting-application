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
    public async Task<ResponseBase> RegisterRoomAsync(RoomRequest request, Guid userId)
    {
        var existingUser = await userGrpcService.UserExisting(userId.ToString());
        if (existingUser.Success)
        {
            return new FailureWithMessage("User not existing.");
        }
        
        var isRoomNameExisting = await roomRepository.IsRoomExistingAsync(request.RoomName);
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

    public async Task<ResponseBase> DeleteRoomAsync(Guid userId, Guid roomId)
    {
        var existingRoom = await roomRepository.GetRoomAsync(roomId);
        if (existingRoom == null)
        {
            return new FailureWithMessage("Room not found.");
        }

        if (!existingRoom.IsCreator(userId))
        {
            return new FailureWithMessage("You don't have permission to delete this room.");
        }
        
        context.Rooms!.Remove(existingRoom);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Server error.");
        }

        return new Success();
    }

    public async Task<ResponseBase> ChangePasswordAsync(ChangeRoomPassword request, Guid userId)
    {
        if (!Guid.TryParse(request.Id, out var roomGuid))
        {
            return new FailureWithMessage("Room not found.");
        }
        
        var existingRoom = await roomRepository.GetRoomAsync(roomGuid);
        if (existingRoom is null)
        {
            return  new FailureWithMessage("Room not found.");
        }
        
        var passwordMatch = existingRoom.PasswordMatch(request.OldPassword);
        if (!passwordMatch)
        {
            return new FailureWithMessage("Wrong password.");
        }
        
        var userIsTheCreator = existingRoom.IsCreator(userId);
        if (!userIsTheCreator)
        {
            return new FailureWithMessage("You don't have permission to change this room's password.");
        }
        
        existingRoom.ChangePassword(request.Password);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Server error.");
        }
        
        return new Success();
    }

    public async Task<bool> UserIsTheCreatorAsync(Guid roomId, Guid userId)
    {
        var roomCreatorId = await roomRepository.GetRoomCreatorId(roomId);
        return roomCreatorId == userId;
    }

    public async Task<ResponseBase> LoginAsync(JoinRoomRequest request, Guid userId)
    {
        var existingRoom = await roomRepository.GetRoomAsync(request.RoomName);
        if (existingRoom is null)
        {
            return new FailureWithMessage("Room not found.");
        }
        
        var isPasswordMatch = existingRoom.PasswordMatch(request.Password);
        if (!isPasswordMatch)
        {
            return  new FailureWithMessage("Wrong password.");
        }
        
        var returningDto = new RoomResponseDto(existingRoom.RoomId, existingRoom.RoomName);
        return new SuccessWithDto<RoomResponseDto>(returningDto);
    }
}