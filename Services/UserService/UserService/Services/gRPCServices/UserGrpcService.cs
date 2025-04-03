using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Model;

namespace UserService.Services.gRPCServices;

public class UserGrpcService(UserManager<ApplicationUser> userManager) : GrpcUserService.GrpcUserServiceBase
{
    public override async Task<UserExistingResponse> UserExisting(UserIdRequest request, ServerCallContext context)
    {
        return new UserExistingResponse {  Success = await userManager.FindByIdAsync(request.Id) != null };
    }
}
