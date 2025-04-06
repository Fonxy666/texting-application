using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Model;
using UserService.Services.EncryptedSymmetricKeyService;

namespace UserService.Services.gRPCServices;

public class UserGrpcService(UserManager<ApplicationUser> userManager, ISymmetricKeyService keyService) : GrpcUserService.GrpcUserServiceBase
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

        try
        {
            await keyService.SaveNewKeyAndLinkToUserAsync(newKey);
            return new BoolResponseWithMessage { Success = true, Message = "Key successfully saved." };
        }
        catch (Exception ex)
        {
            return new BoolResponseWithMessage { Success = false, Message = $"Error: {ex.Message}" };
        }
    }
}
