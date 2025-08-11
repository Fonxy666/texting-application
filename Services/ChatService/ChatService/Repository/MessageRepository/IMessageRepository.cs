using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Message;

namespace ChatService.Repository.MessageRepository;

public interface IMessageRepository
{
    Task<bool> UserIsTheSender(Guid userId, Guid messageId);
    Task<bool> MessageExisting(Guid messageId);
    Task<Message?> GetMessage(Guid messageId);
    Task<IList<MessageDto>?> Get10MessagesWithIndex(GetMessagesRequest request);
    Task<bool> SaveMessage(Message message);
    Task<bool> EditMessage(Message message);
    Task<bool> DeleteMessage(Message message);
}