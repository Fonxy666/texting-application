using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public interface IMessageService
{
    Task<SaveMessageResponse> SendMessage(MessageRequest request);
    Task<IQueryable<Message>> GetLast10Messages(string roomId);
    Task<MessageResponse> EditMessage(EditMessageRequest request);
    Task<MessageResponse> DeleteMessage(string roomId);
}