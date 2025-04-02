using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Model;

namespace UserService.Services.gRPCServices;

public class ProtoUserService(UserManager<ApplicationUser> userManager) : GrpcUserService.GrpcUserServiceBase
{
    public override async Task<UserExistingResponse> UserExisting(GuidRequest request, ServerCallContext context)
    {
        return new UserExistingResponse {  Success = await userManager.FindByIdAsync(request.Guid) != null };
    }
}
