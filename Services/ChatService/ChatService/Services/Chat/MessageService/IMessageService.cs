using ChatService.Model;
using ChatService.Model.Requests.Message;
using ChatService.Model.Responses.Message;

public interface IMessageService
{
    Task<bool> UserIsTheSender(Guid userId, Guid messageId);
    Task<bool> MessageExisting(Guid id);
    Task<SaveMessageResponse> SendMessage(MessageRequest request, string userId);
    Task<IEnumerable<Message>> GetLast10Messages(Guid roomId);
    Task<ChatMessageResponse> EditMessage(EditMessageRequest request);
    Task<ChatMessageResponse> EditMessageSeen(EditMessageSeenRequest request, Guid userId);
    Task<ChatMessageResponse> DeleteMessage(Guid roomId);
}