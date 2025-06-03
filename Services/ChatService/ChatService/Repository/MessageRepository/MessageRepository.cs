using ChatService.Database;
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

    public async Task<IList<MessageDto>?> Get10MessagesWithIndex(Guid roomId, int index)
    {
        return await context.Rooms!
            .SelectMany(r => r.Messages)
            .OrderByDescending(m => m.SendTime)
            .Skip((index - 1) * 10)
            .Take(10)
            .Select(m => new MessageDto(
                m.MessageId,
                m.SenderId,
                m.Text,
                m.SendTime,
                m.SentAsAnonymous,
                m.Seen))
            .ToListAsync();
    }
}