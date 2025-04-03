namespace ChatService.Services.Chat.GrpcService;

public interface IUserGrpcService
{
    public Task<UserExistingResponse> UserExisting(string userId);
}
