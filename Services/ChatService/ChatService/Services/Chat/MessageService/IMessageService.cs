using ChatService.Model.Requests;
using ChatService.Model.Responses.Message;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.MessageService;

public interface IMessageService
{
    Task<SaveMessageResponse> SendMessage(MessageRequest request, string userId);
    Task<ResponseBase> GetLast10Messages(Guid roomId, int index);
    Task<ChatMessageResponse> EditMessage(EditMessageRequest request);
    Task<ChatMessageResponse> EditMessageSeen(EditMessageSeenRequest request, Guid userId);
    Task<ChatMessageResponse> DeleteMessage(Guid roomId);
}