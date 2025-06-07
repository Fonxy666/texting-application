using Grpc.Core;
using UserService.Models;
using UserService.Repository.AppUserRepository;
using UserService.Services.EncryptedSymmetricKeyService;

namespace UserService.Services.gRPCServices;

public class UserGrpcService(ISymmetricKeyService keyService, IUserRepository userRepository) : GrpcUserService.GrpcUserServiceBase
{
    public override async Task<BoolResponseWithMessage> UserExisting(UserIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var userGuid))
        {
            return new BoolResponseWithMessage { Success = false, Message = "Invalid user id." };
        }
        var userExists = await userRepository.IsUserExistingAsync(userGuid);
        if (!userExists)
        {
            return new BoolResponseWithMessage { Success = false, Message = "User not exists." };
        }

        return new BoolResponseWithMessage { Success = true, Message = "User existing" };
    }

    public override async Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(EncryptedRoomIdWithUserId request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userGuid))
        {
            return new BoolResponseWithMessage { Success = true, Message = "Invalid user id." };
        }
        if (!Guid.TryParse(request.RoomId, out var roomGuid))
        {
            return new BoolResponseWithMessage { Success = true, Message = "Invalid room id." };
        }
        var existingUser = await userRepository.IsUserExistingAsync(userGuid);
        if (existingUser)
        {
            return new BoolResponseWithMessage { Success = false, Message = "User not exists." };
        }

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
        if (!Guid.TryParse(request.Id, out var userGuid))
        {
            return new UserIdAndPublicKeyResponse();
        }

        var userIdAndPublicKey = await userRepository.GetUserIdAndPublicKeyAsync(userGuid);
        if (userIdAndPublicKey is null)
        {
            return new UserIdAndPublicKeyResponse();
        }

        return new UserIdAndPublicKeyResponse { UserId = userIdAndPublicKey.Id.ToString(), PublicKey = userIdAndPublicKey.PublicKey };
    }

    public override async Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request, ServerCallContext context)
    {
        var userIdsAndNames = new List<UserIdAndName>();

        foreach (var name in request.Name)
        {
            var existingUser = await userRepository.GetUserIdAndUserNameAsync(name);

            if (existingUser is null)
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