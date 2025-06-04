using ChatService.Model.Requests;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.MessageService;

public interface IMessageService
{
    Task<ResponseBase> SendMessage(MessageRequest request, Guid userId);
    Task<ResponseBase> GetLast10Messages(GetMessagesRequest request);
    Task<ResponseBase> EditMessage(EditMessageRequest request, Guid userId);
    Task<ResponseBase> EditMessageSeen(EditMessageSeenRequest request, Guid userId);
    Task<ResponseBase> DeleteMessage(Guid id, Guid userId);
}