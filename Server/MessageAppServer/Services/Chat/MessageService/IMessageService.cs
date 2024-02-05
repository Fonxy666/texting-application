using Server.Model.Chat;
using Server.Requests;
using Server.Responses;

namespace Server.Services.Chat.MessageService;

public interface IMessageService
{
    Task<MessageResponse> SendMessage(MessageRequest request);
    Task<IQueryable<Message>> GetLast10Messages(string roomId);
}