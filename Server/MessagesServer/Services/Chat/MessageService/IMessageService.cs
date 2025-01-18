using MessagesServer.Model.Chat;
using MessagesServer.Model.Requests.Message;
using MessagesServer.Model.Responses.Message;

namespace MessagesServer.Services.Chat.MessageService;

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