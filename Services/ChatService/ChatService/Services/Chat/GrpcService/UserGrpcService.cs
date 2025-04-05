using Azure;
using ChatService.Model.Requests.EncryptKey;
using Grpc.Net.Client;

namespace ChatService.Services.Chat.GrpcService;

public class UserGrpcService : IUserGrpcService
{
    private readonly Uri _grpcUri = new Uri("https://localhost:7100");
    public async Task<BoolResponseWithMessage> UserExisting(string userId)
    {
        using var channel = GrpcChannel.ForAddress(_grpcUri);
        var client = new GrpcUserService.GrpcUserServiceClient(channel);

        var request = new UserIdRequest {  Id = userId };

        try
        {
            var response = await client.UserExistingAsync(request);
            return new BoolResponseWithMessage { Success = response.Success };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"gRPC Error: {ex.Message}");
            return new BoolResponseWithMessage { Success = false };
        }
    }

    public async Task<BoolResponseWithMessage> SendEncryptedRoomIdForUser(StoreRoomKeyRequest incomingRequest)
    {
        using var channel = GrpcChannel.ForAddress(_grpcUri);
        var client = new GrpcUserService.GrpcUserServiceClient(channel);

        var request = new EncryptedRoomIdWithUserId
        {
            UserId = incomingRequest.UserId.ToString(),
            RoomKey = incomingRequest.EncryptedKey,
            RoomId = incomingRequest.RoomId.ToString()
        };

        try
        {
            var response = await client.SendEncryptedRoomIdForUserAsync(request);
            return new BoolResponseWithMessage { Success = response.Success };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"gRPC Error: {ex.Message}");
            return new BoolResponseWithMessage { Success = false };
        }
    }
}
