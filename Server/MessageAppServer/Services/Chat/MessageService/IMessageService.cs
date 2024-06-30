using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

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