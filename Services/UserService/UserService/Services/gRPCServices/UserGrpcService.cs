using Grpc.Core;
using UserService.Models;
using UserService.Services.EncryptedSymmetricKeyService;
using UserService.Helpers;

namespace UserService.Services.gRPCServices;

public class UserGrpcService(ISymmetricKeyService keyService, IUserHelper userHelper) : GrpcUserService.GrpcUserServiceBase
{
    public override async Task<BoolResponseWithMessage> UserExisting(UserIdRequest request, ServerCallContext context)
    {
        BoolResponseWithMessage OnSuccess(ApplicationUser existingUser) => new() { Success = true, Message = "User exists" };

        BoolResponseWithMessage OnFailure(string message) => new() { Success = false, Message = message };
        
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            request.Id,
            (Func<ApplicationUser, BoolResponseWithMessage>)OnSuccess,
            (Func<string, BoolResponseWithMessage>)OnFailure
        );
    }
    
    public override async Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(EncryptedRoomIdWithUserId request, ServerCallContext context)
    {
        async Task<BoolResponseWithMessage> OnSuccess(ApplicationUser existingUser)
        {
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

        BoolResponseWithMessage OnFailure(string message) => new() { Success = false, Message = message };

        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            request.UserId,
            OnSuccess,
            OnFailure
        );
    }

    public override async Task<UserIdAndPublicKeyResponse> SendUserPublicKeyAndId(UserIdRequest request, ServerCallContext context)
    {
        UserIdAndPublicKeyResponse OnSuccess(ApplicationUser existingUser) => new() { UserId = existingUser.Id.ToString(), PublicKey = existingUser.PublicKey };

        UserIdAndPublicKeyResponse OnFailure(string message) => new();
        
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            request.Id,
            OnSuccess,
            OnFailure
        );
    }

    public override async Task<UsersResponse> SendUserNamesAndGetIds(UserNamesRequest request, ServerCallContext context)
    {
        var userIdsAndNames = new List<UserIdAndName>();

        Task<BoolResponseWithMessage> OnSuccess(ApplicationUser user)
        {
            userIdsAndNames.Add(new UserIdAndName { Name = user.UserName, Id = user.Id.ToString() });
            return Task.FromResult(new BoolResponseWithMessage { Success = true, Message = "User found" });
        }
        
        BoolResponseWithMessage OnFailure(string message) => new() { Success = false, Message = message };

        if (request.Name == null || request.Name.Count == 0)
        {
            return new UsersResponse();
        }
        
        foreach (var name in request.Name)
        {
            await userHelper.GetUserOrFailureResponseAsync(
                UserIdentifierType.Username,
                name,
                OnSuccess,
                OnFailure
            );
        }

        var response = new UsersResponse
        {
            Users = { userIdsAndNames }
        };

        return response;
    }
}
