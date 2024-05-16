using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public class MessageService(DatabaseContext context) : IMessageService
{
    private DatabaseContext Context { get; } = context;
    public Task<bool> MessageExisting(Guid id)
    {
        return context.Messages!.AnyAsync(message => message.MessageId == id);
    }

    public async Task<SaveMessageResponse> SendMessage(MessageRequest request)
    {
        var roomIdToGuid = new Guid(request.RoomId);
        var userIdToGuid = new Guid(request.UserId);
        var message = request.MessageId != null ? 
            new Message(roomIdToGuid, userIdToGuid, request.Message, new Guid(request.MessageId), request.AsAnonymous) : 
            new Message(roomIdToGuid, userIdToGuid, request.Message, request.AsAnonymous);
        
        await Context.Messages!.AddAsync(message);
        await Context.SaveChangesAsync();

        return new SaveMessageResponse(true, message, null);
    }

    public async Task<IQueryable<Message>> GetLast10Messages(Guid roomId)
    {
        var messages = await Context.Messages!
            .Where(message => message.RoomId == roomId)
            .OrderByDescending(message => message.SendTime)
            .Take(10)
            .OrderBy(message => message.SendTime)
            .ToListAsync();
        return messages.AsQueryable();
    }
    
    public async Task<MessageResponse> EditMessage(EditMessageRequest request)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == request.Id);
        
        existingMessage!.ChangeMessageText(request.Message);

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> EditMessageSeen(EditMessageSeenRequest request)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == new Guid(request.MessageId));
        
        existingMessage!.AddUserToSeen(new Guid(request.UserId));

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> DeleteMessage(Guid id)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == id);
        
        Context.Messages!.Remove(existingMessage!);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, id.ToString(), null);
    }

    public async Task DeleteMessages(Guid roomId)
    {
        await context.Messages!.Where(message => message.RoomId == roomId).ExecuteDeleteAsync();
    }
}