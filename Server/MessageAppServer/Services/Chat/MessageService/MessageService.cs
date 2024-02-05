using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Requests;
using Server.Responses;

namespace Server.Services.Chat.MessageService;

public class MessageService(MessagesContext context) : IMessageService
{
    private MessagesContext Context { get; } = context;
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

    public async Task<IQueryable<Message>> GetLast10Messages(string roomId)
    {
        var messages = await Context.Messages
            .Where(message => message.RoomId == roomId)
            .OrderBy(message => message.SendTime)
            .Take(10)
            .ToListAsync();
        return messages.AsQueryable();
    }
}