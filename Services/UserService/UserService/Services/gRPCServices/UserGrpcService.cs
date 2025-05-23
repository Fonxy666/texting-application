using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Models;
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

    public override async Task<UserIdAndPublicKeyResponse> SendUserPublicKeyAndId(UserIdRequest request, ServerCallContext context)
    {
        var existingUser = await userManager.FindByIdAsync(request.Id);
        if (existingUser == null)
        {
            return new UserIdAndPublicKeyResponse { };
        }

        return new UserIdAndPublicKeyResponse { UserId = existingUser.Id.ToString(), PublicKey = existingUser.PublicKey};
    }

    public override async Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request, ServerCallContext context)
    {
        var userIdsAndNames = new List<UserIdAndName>();

        foreach (var name in request.Name)
        {
            var existingUser = await userManager.FindByNameAsync(name);

            if (existingUser == null)
            {
                continue;
            }

            userIdsAndNames.Add(new UserIdAndName { Name = existingUser.UserName, Id = existingUser.Id.ToString() });
        }

        var response = new UsersResponse
        {
            Users = { userIdsAndNames }
        };

        return response;
    }
}