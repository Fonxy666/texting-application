using ChatService.Model;
using ChatService.Model.Requests.Message;
using ChatService.Model.Responses.Message;

public interface IMessageService
{
    Task<bool> UserIsTheSender(Guid userId, Guid messageId);
    Task<bool> MessageExisting(Guid id);
    Task<SaveMessageResponse> SendMessage(MessageRequest request, string userId);
    Task<IQueryable<Message>> GetLast10Messages(Guid roomId);
    Task<MessageResponse> EditMessage(EditMessageRequest request);
    Task<MessageResponse> EditMessageSeen(EditMessageSeenRequest request, Guid userId);
    Task<MessageResponse> DeleteMessage(Guid roomId);
}