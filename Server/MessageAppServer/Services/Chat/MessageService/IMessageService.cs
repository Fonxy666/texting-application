using Server.Model.Chat;

namespace Server.Services.Chat.MessageService;

public interface IMessageService
{
    Task<MessageResponse> SendMessage(MessageRequest request);
    Task<IQueryable<Message>> GetLast10Messages(string roomId);
}