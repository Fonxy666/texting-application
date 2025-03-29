using Grpc.Core;

namespace UserService.Services.gRPCServices;

public class AuthenticationService : UserAuthenticationService.UserAuthenticationServiceBase
{
    public override Task<MessageResponse> GetMessage(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new MessageResponse { Message = "It's working!" });
    }
}
