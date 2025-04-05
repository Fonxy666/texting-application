using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Model;
using UserService.Services.User;

namespace UserService.Services.gRPCServices;

public class UserGrpcService(UserManager<ApplicationUser> userManager, IUserServices userServices) : GrpcUserService.GrpcUserServiceBase
{
    public override async Task<BoolResponseWithMessage> UserExisting(UserIdRequest request, ServerCallContext context)
    {
        var userExists = await userManager.FindByIdAsync(request.Id);
        if (userExists == null)
        {
            return new BoolResponseWithMessage { Success = false, Message = "User not exists." };
        }

        return new BoolResponseWithMessage {  Success = true, Message = "User existing" };
    }

    public override async Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(EncryptedRoomIdWithUserId request, ServerCallContext context)
    {
        var existingUser = await userManager.FindByIdAsync(request.UserId);
        if (existingUser == null)
        {
            return new BoolResponseWithMessage { Success = false, Message = "User not exists." };
        }

        var userGuid = Guid.Parse(request.UserId);
        var roomGuid = Guid.Parse(request.RoomId);
        var newKey = new EncryptedSymmetricKey(userGuid, request.RoomKey, roomGuid);

        var updatedUser = await userManager.FindByIdAsync(request.UserId);
        if (updatedUser == null)
        {
            return new BoolResponseWithMessage { Success = false, Message = "User not found after key creation." };
        }

        try
        {
            await userServices.AddNewKeyAsync(userGuid, roomGuid, newKey);
            await userServices.AddNewRoomToUser(userGuid, roomGuid, newKey);
            return new BoolResponseWithMessage { Success = true, Message = "User successfully updated." };
        }
        catch (Exception ex)
        {
            return new BoolResponseWithMessage { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
