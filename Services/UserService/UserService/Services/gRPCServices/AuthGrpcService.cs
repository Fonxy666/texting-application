using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Model;
using UserService.Services.Authentication;

namespace UserService.Services.gRPCServices;

public class AuthGrpcService(UserManager<ApplicationUser> userManager, ITokenService tokenService) : GrpcAuthService.GrpcAuthServiceBase
{
    public override async Task<JwtResponse> NewJwtToken(GrpcNewJwtRequest request, ServerCallContext context)
    {
        var existingUser = await userManager.FindByIdAsync(request.UserId.ToString());
        if (existingUser == null)
        { 
            return new JwtResponse { JwtToken = "User not existing." };
        }

        var newToken = tokenService.CreateJwtToken(existingUser!, "User", request.Remember);
        return new JwtResponse { JwtToken = newToken };
    }
}
