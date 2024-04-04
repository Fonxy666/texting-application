using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public class MessageService(MessagesContext context) : IMessageService
{
    private MessagesContext Context { get; } = context;
    public async Task<MessageResponse> SendMessage(MessageRequest request)
    {
        try
        {
            var message = request.MessageId != null ? 
                new Message(request.RoomId, request.UserName, request.Message, request.MessageId, request.AsAnonymous) : 
                new Message(request.RoomId, request.UserName, request.Message, request.AsAnonymous);
            
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
            .OrderByDescending(message => message.SendTime)
            .Take(10)
            .OrderBy(message => message.SendTime)
            .ToListAsync();
        return messages.AsQueryable();
    }
    
    public async Task<MessageResponse> EditMessage(EditMessageRequest request)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == request.Id);
        
        if (existingMessage!.RoomId.Length < 1)
        {
            return new MessageResponse(false, "");
        }
        
        try
        {
            existingMessage.RoomId = request.RoomId;
            existingMessage.SenderId = request.Id;
            existingMessage.Text = request.Message;

            Context.Messages.Update(existingMessage);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, request.RoomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "");
        }
    }

    public async Task<MessageResponse> DeleteMessage(string id)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == id);
        
        if (existingMessage!.RoomId.Length < 1)
        {
            return new MessageResponse(false, "");
        }
        
        try
        {
            Context.Messages.Remove(existingMessage);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, id);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "");
        }
    }
}