using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Services.User;

public class ApplicationUserService(UserManager<ApplicationUser> userManager, IConfiguration configuration, MainDatabaseContext context) : IApplicationUserService
{
    public Task<bool> ExamineUserExistingWithIdAsync(string id)
    {
        return userManager.Users.AnyAsync(user => user.Id.ToString() == id);
    }

    public async Task<ResponseBase> ExamineUserNotExistingAsync(string UserName, string Email)
    {
        var userWithSameEmail = await userManager.FindByEmailAsync(Email);
        if (userWithSameEmail != null)
        {
            return new FailedAuthResultWithMessage("This E-mail address is already taken.");
        }

        var userWithSameUserName = await userManager.FindByEmailAsync(UserName);
        if (userWithSameEmail != null)
        {
            return new FailedAuthResultWithMessage("This Username is already taken.");
        }

        return new AuthResponseSuccess();
    }

    public Task<ApplicationUser> GetUserWithSentRequestsAsync(string userId)
    {
        var userGuid = new Guid(userId);

        return Task.FromResult(userManager.Users
            .Include(u => u.SentFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid).Result!);
    }

    public Task<ApplicationUser> GetUserWithReceivedRequestsAsync(string userId)
    {
        var userGuid = new Guid(userId);

        return Task.FromResult(userManager.Users
            .Include(u => u.SentFriendRequests)
            .FirstOrDefaultAsync(u => u.Id == userGuid).Result!);
    }
    public async Task<ResponseBase> GetUserPrivatekeyForRoomAsync(string userId, string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            return new FailedUserResponseWithMessage("Invalid Roomid format.");
        }

        var userGuid = new Guid(userId);
        var existingUser = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .FirstOrDefaultAsync(u => u.Id == userGuid && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

        if (existingUser == null)
        {
            return new FailedUserResponseWithMessage("Cannot find the key for this user.");
        }

        var userKey = existingUser.UserSymmetricKeys.FirstOrDefault(key => key.RoomId == roomGuid)!.ToString();

        return new KeyResponseSuccess(userKey!);
    }

    public async Task<ResponseBase> GetRoommatePublicKey(string userName)
    {
        var existingUser = await userManager.FindByNameAsync(userName);
        if (existingUser == null)
        {
            return new FailedUserResponseWithMessage($"There is no user with this Username: {userManager}");
        }

        return new KeyResponseSuccess(existingUser.PublicKey);
    }
    public async Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string userName, string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            return new FailedUserResponseWithMessage("Invalid Roomid format.");
        }

        var userExisting = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .AnyAsync(u => u.UserName == userName && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

        if (!userExisting)
        {
            return new FailedUserResponseWithMessage($"There is no key or user with this Username: {userName}");
        }

        return new UserResponseSuccess();
    }

    public string SaveImageLocally(string usernameFileName, string base64Image)
    {
        var folderPath = configuration["ImageFolderPath"]??Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = usernameFileName + ".png";
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

    public async 

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