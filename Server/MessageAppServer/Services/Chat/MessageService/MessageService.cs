﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public class MessageService(MessagesContext context, RoomsContext roomsContext) : IMessageService
{
    private MessagesContext Context { get; } = context;
    private RoomsContext RoomsContext { get; } = roomsContext;
    public async Task<SaveMessageResponse> SendMessage(MessageRequest request)
    {
        var message = request.MessageId != null ? 
            new Message(request.RoomId, request.UserName, request.Message, request.MessageId, request.AsAnonymous) : 
            new Message(request.RoomId, request.UserName, request.Message, request.AsAnonymous);
        
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

        if (existingMessage == null)
        {
            return new MessageResponse(false, "", $"There is no message with id: {request.Id}");
        }
        
        if (existingMessage!.RoomId.Length < 1)
        {
            return new MessageResponse(false, "", $"There is no room with the id: {request.Id}");
        }
        
        try
        {
            existingMessage.Text = request.Message;

            Context.Messages.Update(existingMessage);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, "", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "", "Unexpected error happened.");
        }
    }

    public async Task<MessageResponse> EditMessageSeen(EditMessageSeenRequest request)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == request.messageId);
        
        try
        {
            var sawId = new Guid(request.userId);
            existingMessage.AddUserToSeen(sawId);

            Context.Messages.Update(existingMessage);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, "", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "", "Unexpected error happened.");
        }
    }

    public async Task<MessageResponse> DeleteMessage(string id)
    {
        var existingMessage = Context.Messages.FirstOrDefault(message => message.MessageId == id);
        
        if (existingMessage!.RoomId.Length < 1)
        {
            return new MessageResponse(false, "", null);
        }
        
        try
        {
            Context.Messages.Remove(existingMessage);
            await Context.SaveChangesAsync();

            return new MessageResponse(true, id, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new MessageResponse(false, "", null);
        }
    }
}