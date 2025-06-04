using ChatService.Database;
using ChatService.Model.Responses.Message;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Repository.MessageRepository;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using Textinger.Shared.Responses;

namespace ChatService.Services.Chat.MessageService;

public class MessageService(
    IMessageRepository messageRepository,
    IRoomRepository roomRepository,
    IUserGrpcService userGrpcService
    ) : IMessageService
{
    public async Task<ResponseBase> SendMessage(MessageRequest request, Guid userId)
    {
        var isUserExisting = await userGrpcService.UserExisting(userId.ToString());
        if (!isUserExisting.Success)
        {
            return new FailureWithMessage("User not found.");
        }
        var isRoomExisting = await roomRepository.IsRoomExistingAsync(request.RoomId);
        if (!isRoomExisting)
        {
            return new FailureWithMessage("Room not found.");
        }
        
        var message = request.MessageId != null ? 
            new Message(request.RoomId, userId, request.Message, new Guid(request.MessageId), request.AsAnonymous, request.Iv) : 
            new Message(request.RoomId, userId, request.Message, request.AsAnonymous, request.Iv);
        
        
        var result = await messageRepository.SaveMessage(message);
        if (!result)
        {
            return new FailureWithMessage("Database error.");
        }

        return new SuccessWithDto<Message>(message);
    }

    public async Task<ResponseBase> GetLast10Messages(GetMessagesRequest request)
    {
        var messages = await messageRepository.Get10MessagesWithIndex(request);
        if (messages is null)
        {
            return new FailureWithMessage("There is no room with the given id.");
        }
        
        return new SuccessWithDto<IList<MessageDto>>(messages);
    }
    
    public async Task<ResponseBase> EditMessage(EditMessageRequest request, Guid userId)
    {
        var existingMessage = await messageRepository.GetMessage(request.Id);
        if (existingMessage is null)
        {
            return new FailureWithMessage("There is no message with the given id.");
        }

        if (existingMessage.SenderId != userId)
        {
            return new FailureWithMessage("You don't have permission.");
        }
        
        existingMessage.ChangeMessageText(request.Message);
        existingMessage.ChangeMessageIv(request.Iv);

        var result = await messageRepository.EditMessage(existingMessage);
        if (!result)
        {
            return  new FailureWithMessage("Database error.");
        }

        return new Success();
    }

    public async Task<ResponseBase> EditMessageSeen(EditMessageSeenRequest request, Guid userId)
    {
        var existingMessage = await messageRepository.GetMessage(request.MessageId);
        if (existingMessage is null)
        {
            return new FailureWithMessage("There is no message with the given id.");
        }
        
        existingMessage.AddUserToSeen(userId);

        var result = await messageRepository.EditMessage(existingMessage);
        if (!result)
        {
            return  new FailureWithMessage("Database error.");
        }

        return new Success();
    }

    public async Task<ResponseBase> DeleteMessage(Guid id, Guid userId)
    {
        var existingMessage = await messageRepository.GetMessage(id);
        if (existingMessage is null)
        {
            return new FailureWithMessage("There is no message with the given id.");
        }
        
        if (existingMessage.SenderId != userId)
        {
            return new FailureWithMessage("You don't have permission.");
        }

        var result = await messageRepository.DeleteMessage(existingMessage);
        if (!result)
        {
            return  new FailureWithMessage("Database error.");
        }

        return new Success();
    }
}