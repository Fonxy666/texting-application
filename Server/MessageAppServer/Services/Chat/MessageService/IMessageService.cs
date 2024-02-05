namespace Server.Services.Chat.MessageService;

public interface IMessageService
{
    Task<MessageResponse> SendMessage(MessageRequest request);
}