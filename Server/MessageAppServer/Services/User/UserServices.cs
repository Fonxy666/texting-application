using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using AuthenticationServer.Database;
using AuthenticationServer.Model;
using AuthenticationServer.Model.Responses.User;

namespace AuthenticationServer.Services.User;

public class UserServices(UserManager<ApplicationUser> userManager, IConfiguration configuration, MainDatabaseContext context) : IUserServices
{
    public Task<bool> ExistingUser(string id)
    {
        return userManager.Users.AnyAsync(user => user.Id.ToString() == id);
    }

    public Task<ApplicationUser> GetUserWithSentRequests(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid id format.");
        }

        return Task.FromResult(userManager.Users
            .Include(u => u.SentFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid).Result!);
    }

    public Task<ApplicationUser> GetUserWithReceivedRequests(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid id format.");
        }

        return Task.FromResult(userManager.Users
            .Include(u => u.SentFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid).Result!);
    }

    public string SaveImageLocally(string userNameFileName, string base64Image)
    {
        var folderPath = configuration["ImageFolderPath"]??Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = userNameFileName + ".png";
        var imagePath = Path.Combine(folderPath, imageName);

        try
        {
            if (base64Image.Length <= 1)
            {
                return "";
            }
            
            base64Image = base64Image.Replace("data:image/png;base64,", "");
            var imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
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
    }

    public string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    public async Task<DeleteUserResponse> DeleteAsync(ApplicationUser user)
    {
        var sentFriendRequests = context.FriendConnections.Where(fc => fc.SenderId == user.Id);
        var receivedFriendRequests = context.FriendConnections.Where(fc => fc.ReceiverId == user.Id);

        foreach (var friendRequest in sentFriendRequests)
        {
            var receiver = await userManager.FindByIdAsync(friendRequest.ReceiverId.ToString());
            if (receiver != null)
            {
                await context.Entry(receiver).Collection(u => u.Friends).LoadAsync();
                receiver.Friends.Remove(user);
            }
        }

        foreach (var friendRequest in receivedFriendRequests)
        {
            var sender = await userManager.FindByIdAsync(friendRequest.SenderId.ToString());
            if (sender != null)
            {
                await context.Entry(sender).Collection(u => u.Friends).LoadAsync();
                sender.Friends.Remove(user);
            }
        }

        context.FriendConnections.RemoveRange(sentFriendRequests);
        context.FriendConnections.RemoveRange(receivedFriendRequests);

        await userManager.DeleteAsync(user);

        return new DeleteUserResponse($"{user.UserName}", "Delete successful.", true);
    }
}