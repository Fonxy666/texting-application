using Grpc.Core;
using UserService.Helpers;
using UserService.Models;
using UserService.Services.Authentication;

namespace UserService.Services.gRPCServices;

public class AuthGrpcService(ITokenService tokenService, IUserHelper userHelper) : GrpcAuthService.GrpcAuthServiceBase
{
    public override async Task<JwtResponse> NewJwtToken(GrpcNewJwtRequest request, ServerCallContext context)
    {
        JwtResponse OnSuccess(ApplicationUser existingUser)
        {
            var newToken = tokenService.CreateJwtToken(existingUser, "User", request.Remember);
            return new JwtResponse { JwtToken = newToken };
        }

        JwtResponse OnFailure(string message) => new() { JwtToken = "User not existing." };
        
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            request.UserId,
            (Func<ApplicationUser, JwtResponse>)OnSuccess,
            (Func<string, JwtResponse>)OnFailure
        );
    }
}
