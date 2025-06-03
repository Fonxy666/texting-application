using ChatService.Model.Responses.Message;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Repository.MessageRepository;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.MessageService;

public class MessageService(IMessageRepository messageRepository) : IMessageService
{
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

    public async Task<ResponseBase> GetLast10Messages(Guid roomId, int index)
    {
        var messages = await messageRepository.Get10MessagesWithIndex(roomId, index);
        if (messages is null)
        {
            return new FailureWithMessage("There is no room with the given id.");
        }
        
        return new SuccessWithDto<IList<MessageDto>>(messages);
        
        
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