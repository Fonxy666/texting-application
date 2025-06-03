using ChatService.Model;
using ChatService.Model.Responses.Message;

namespace ChatService.Repository.MessageRepository;

public interface IMessageRepository
{
    Task<bool> UserIsTheSender(Guid userId, Guid messageId);
    Task<bool> MessageExisting(Guid messageId);
    Task<IList<MessageDto>?> Get10MessagesWithIndex(Guid roomId, int index);
}