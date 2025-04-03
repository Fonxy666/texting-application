using Grpc.Net.Client;

namespace ChatService.Services.Chat.GrpcService;

public class UserGrpcService : IUserGrpcService
{
    public async Task<UserExistingResponse> UserExisting(string userId)
    {
        var grpcUri = new Uri("https://localhost:7100");
        using var channel = GrpcChannel.ForAddress(grpcUri);
        var client = new GrpcUserService.GrpcUserServiceClient(channel);

        var request = new UserIdRequest {  Id = userId };

        try
        {
            var response = await client.UserExistingAsync(request);
            return new UserExistingResponse { Success = response.Success };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"gRPC Error: {ex.Message}");
            return new UserExistingResponse { Success = false };
        }
    }
}
