using Server.Database;
using Server.Model.Chat;

namespace Server.Services.Chat.MessageService;

public class MessageService(MessagesContext context) : IMessageService
{
    private MessagesContext Context { get; set; } = context;
    public async Task<MessageResponse> SendMessage(MessageRequest request)
    {
        try
        {
            var message = new Message(request.RoomId, request.UserName, request.Message);
            await Context.Messages.AddAsync(message);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, request.RoomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "");
        }
    }
}