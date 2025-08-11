using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using UserService.Models;
using UserService.Services.Authentication;

namespace UserService.Services.gRPCServices;

public class AuthGrpcService(ITokenService tokenService, UserManager<ApplicationUser> userManager) : GrpcAuthService.GrpcAuthServiceBase
{
    public override async Task<JwtResponse> NewJwtToken(GrpcNewJwtRequest request, ServerCallContext context)
    {
        var existingUser = await userManager.FindByIdAsync(request.UserId);
        if (existingUser is null)
        {
            return new JwtResponse { JwtToken = "User not existing." };
        }
        
        var newToken = tokenService.CreateJwtToken(existingUser, "User", request.Remember);
        
        return new JwtResponse { JwtToken = newToken };
    }
}
