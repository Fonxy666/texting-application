using ChatService.Model.Requests;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.RoomService;

public interface IRoomService
{
    Task<ResponseBase> RegisterRoomAsync(RoomRequest request, Guid userId);
    Task<ResponseBase> DeleteRoomAsync(Guid userId, Guid roomId);
    Task<ResponseBase> ChangePasswordAsync(ChangeRoomPassword request, Guid userId);
    Task<bool> UserIsTheCreatorAsync(Guid userId, Guid roomId);
    Task<ResponseBase> LoginAsync(JoinRoomRequest request, Guid userId);
}