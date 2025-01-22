using System.IdentityModel.Tokens.Jwt;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using AuthenticationServer.gRPC;

namespace AuthenticationServer.gRPC
{
    public class GrpcServer : AuthService.AuthServiceBase // Használd a helyes osztályt (AuthServiceBase)
    {
        public override Task<JwtTokenResponse> ValidateJwtToken(JwtTokenRequest request, ServerCallContext context)
        {
            // Your JWT validation logic
            var isValid = ValidateJwt(request.Token);
            return Task.FromResult(new JwtTokenResponse { IsValid = isValid });
        }

        private bool ValidateJwt(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                return jsonToken != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}