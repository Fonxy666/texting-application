﻿using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public class MessageService(MessagesContext context) : IMessageService
{
    private MessagesContext Context { get; } = context;
    public Task<bool> MessageExisting(string id)
    {
        return context.Messages.AnyAsync(message => message.MessageId == id);
    }

    public async Task<SaveMessageResponse> SendMessage(MessageRequest request)
    {
        var message = request.MessageId != null ? 
            new Message(request.RoomId, request.UserId, request.Message, request.MessageId, request.AsAnonymous) : 
            new Message(request.RoomId, request.UserId, request.Message, request.AsAnonymous);
        
        await Context.Messages.AddAsync(message);
        await Context.SaveChangesAsync();

        return new SaveMessageResponse(true, message, null);
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
        
        existingMessage!.Text = request.Message;

        Context.Messages.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> EditMessageSeen(EditMessageSeenRequest request)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == request.MessageId);
        
        var sawId = new Guid(request.UserId);
        existingMessage!.AddUserToSeen(sawId);

        Context.Messages.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> DeleteMessage(string id)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == id);
        
        Context.Messages.Remove(existingMessage!);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, id, null);
    }
}