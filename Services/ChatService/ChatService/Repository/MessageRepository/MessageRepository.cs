using ChatService.Database;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Message;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Repository.MessageRepository;

public class MessageRepository(ChatContext context) : IMessageRepository
{
    public async Task<bool> UserIsTheSender(Guid userId, Guid messageId)
    {
        var senderId = await context.Messages!
            .Where(m => m.MessageId == messageId)
            .Select(m => m.SenderId)
            .FirstOrDefaultAsync();

        return senderId == userId;
    }

    public async Task<bool> MessageExisting(Guid messageId)
    {
        return await context.Messages!.AnyAsync(m => m.MessageId == messageId);
    }

    public async Task<Message?> GetMessage(Guid messageId)
    {
        return await context.Messages!.FirstOrDefaultAsync(m => m.MessageId == messageId);
    }

    public async Task<IList<MessageDto>?> Get10MessagesWithIndex(GetMessagesRequest request)
    {
        return await context.Rooms!
            .Where(m => m.RoomId == request.RoomId)
            .SelectMany(r => r.Messages)
            .OrderByDescending(m => m.SendTime)
            .Skip((request.Index - 1) * 10)
            .Take(10)
            .Select(m => new MessageDto(
                m.MessageId,
                m.SenderId,
                m.Text,
                m.SendTime,
                m.SentAsAnonymous,
                m.Seen,
                m.Iv))
            .ToListAsync();
    }

    public async Task<bool> SaveMessage(Message message)
    {
        await context.Messages!.AddAsync(message);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EditMessage(Message message)
    {
        context.Messages!.Update(message);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteMessage(Message message)
    {
        context.Messages!.Remove(message);
        return await context.SaveChangesAsync() > 0;
    }
}