using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Services.Chat.MessageService;

public class MessageService(MainDatabaseContext context, IConfiguration configuration) : IMessageService
{
    private MainDatabaseContext Context { get; } = context;
    public async Task<bool> UserIsTheSender(Guid userId, Guid messageId)
    {
        var message = await Context.Messages!.FirstOrDefaultAsync(m => m.ItemId == messageId);
        return message!.SenderId == userId;
    }

    public Task<bool> MessageExisting(Guid id)
    {
        return Context.Messages!.AnyAsync(message => message.ItemId == id);
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

    public async Task<SaveImageResponse> SendImage(ImageRequest request, string userId)
    {
        var roomIdToGuid = new Guid(request.RoomId);
        var userIdToGuid = new Guid(userId);
        var messageId = Guid.NewGuid();
        
        var imagePath = SaveImageLocally(messageId.ToString(), request.RoomId, request.Message);
        
        var image = request.MessageId != null ? 
            new Image(roomIdToGuid, userIdToGuid, imagePath, new Guid(request.MessageId), request.AsAnonymous, request.Iv) : 
            new Image(roomIdToGuid, userIdToGuid, imagePath, messageId, request.AsAnonymous, request.Iv);
        
        await Context.Messages!.AddAsync(image);
        await Context.SaveChangesAsync();

        return new SaveImageResponse(true, image, null);
    }
    
    private string SaveImageLocally(string imageId, string roomId, string base64Image)
    {
        var folderPath = configuration["ImageFolderPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), $"RoomImages/{roomId}");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = imageId + ".png";
        var imagePath = Path.Combine(folderPath, imageName);

        try
        {
            if (string.IsNullOrWhiteSpace(base64Image) || base64Image.Length <= 1)
            {
                return string.Empty;
            }

            byte[] imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            return imagePath;
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding base64 image: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving image to file system: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<ItemBase>> GetLast10Messages(Guid roomId)
{
    var messages = await Context.Messages!
        .Where(message => message.RoomId == roomId)
        .OrderByDescending(message => message.SendTime)
        .Take(10)
        .OrderBy(message => message.SendTime)
        .ToListAsync();

    var returningList = new List<ItemBase>();
    
    foreach (var itemBase in messages)
    {
        switch (itemBase)
        {
            case Image image:
            {
                var newImage = new Image
                {
                    ItemId = itemBase.ItemId,
                    Iv = image.Iv,
                    RoomId = image.RoomId,
                    Seen = image.Seen,
                    SenderId = image.SenderId,
                    SendTime = image.SendTime,
                    SentAsAnonymous = image.SentAsAnonymous,
                    ImagePath = image.ImagePath
                };
                returningList.Add(newImage);
                break;
            }
            case Message message:
            {
                var newMessage = new Message
                {
                    ItemId = itemBase.ItemId,
                    Iv = message.Iv,
                    RoomId = message.RoomId,
                    Seen = message.Seen,
                    SenderId = message.SenderId,
                    SendTime = message.SendTime,
                    SentAsAnonymous = message.SentAsAnonymous,
                    Text = message.Text
                };
                returningList.Add(newMessage);
                break;
            }
        }
    }

    return returningList;
}
    
    public async Task<MessageResponse> EditMessage(EditMessageRequest request)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.ItemId == request.Id) as Message;
        
        existingMessage!.ChangeMessageText(request.Message);
        existingMessage!.ChangeMessageIv(request.Iv);

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> EditMessageSeen(EditMessageSeenRequest request, Guid userId)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.ItemId == new Guid(request.MessageId));
        
        existingMessage!.AddUserToSeen(userId);

        Context.Messages!.Update(existingMessage);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, "", null);
    }

    public async Task<MessageResponse> DeleteMessage(Guid id)
    {
        var existingMessage = Context.Messages!.FirstOrDefault(message => message.ItemId == id);
        
        Context.Messages!.Remove(existingMessage!);
        await Context.SaveChangesAsync();

        return new MessageResponse(true, id.ToString(), null);
    }
}