using Server.Model.Chat;
using Server.Requests;
using Server.Requests.Message;
using Server.Responses;
using Server.Responses.Message;

namespace Server.Services.Chat.MessageService;

public interface IMessageService
{
    Task<MessageResponse> SendMessage(MessageRequest request);
    Task<IQueryable<Message>> GetLast10Messages(string roomId);
    Task<MessageResponse> EditMessage(EditMessageRequest request);
    Task<MessageResponse> DeleteMessage(string roomId);
}