using Microsoft.EntityFrameworkCore;
using ChatService.Model.Responses.Message;
using ChatService.Model;
using ChatService.Database;
using ChatService.Model.Requests;

namespace ChatService.Services.Chat.MessageService;

public class MessageService(ChatContext context) : IMessageService
{
    private ChatContext Context { get; } = context;
    public async Task<bool> UserIsTheSender(Guid userId, Guid messageId)
    {
        var message = await Context.Messages!.FirstOrDefaultAsync(m => m.MessageId == messageId);
        return message!.SenderId == userId;
    }

    public Task<bool> MessageExisting(Guid id)
    {
        return Context.Messages!.AnyAsync(message => message.MessageId == id);
    }

    public async Task<SaveMessageResponse> SendMessage(MessageRequest request, string userId)
    {
        var roomIdToGuid = new Guid(request.RoomId);
        var userIdToGuid = new Guid(userId);
        var message = request.MessageId != null ? 
            new Message(roomIdToGuid, userIdToGuid, request.Message, new Guid(request.MessageId), request.AsAnonymous, request.Iv) : 
            new Message(roomIdToGuid, userIdToGuid, request.Message, request.AsAnonymous, request.Iv);
        
        await Context.Messages!.AddAsync(message);
        await Context.SaveChangesAsync();

        return new SaveMessageResponse(true, message, null);
    }

    public async Task<IEnumerable<Message>> GetLast10Messages(Guid roomId)
    {
       return await Context.Messages
        .Where(m => m.RoomId == roomId)
        .OrderByDescending(m => m.SendTime)
        .Take(10)
        .ToListAsync();
    }
    
    public async Task<ChatMessageResponse> EditMessage(EditMessageRequest request)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == request.Id);
        
        existingMessage!.ChangeMessageText(request.Message);
        existingMessage!.ChangeMessageIv(request.Iv);

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new ChatMessageResponse(true, "", null);
    }

    public async Task<ChatMessageResponse> EditMessageSeen(EditMessageSeenRequest request, Guid userId)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == new Guid(request.MessageId));
        
        existingMessage!.AddUserToSeen(userId);

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new ChatMessageResponse(true, "", null);
    }

    public async Task<ChatMessageResponse> DeleteMessage(Guid id)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.MessageId == id);
        
        Context.Messages!.Remove(existingMessage!);
        await Context.SaveChangesAsync();

        return new ChatMessageResponse(true, id.ToString(), null);
    }
}