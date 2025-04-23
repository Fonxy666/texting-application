using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.EmailSender;

namespace UserService.Services.User;

public class ApplicationUserService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    MainDatabaseContext context,
    IEmailSender emailSender,
    MainDatabaseContext repository,
    IAuthService authService
    ) : IApplicationUserService
{public async Task<ResponseBase> GetUserNameAsync(string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        return new UserResponseSuccessWithMessage(existingUser.UserName!);
    }

    public async Task<ResponseBase> GetImageWithIdAsync(string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        var folderPath = configuration["ImageFolderPath"] ??
                         Path.Combine(Directory.GetCurrentDirectory(), "Avatars");

        var imagePath = Path.Combine(folderPath, $"{existingUser!.UserName}.png");

        if (!File.Exists(imagePath))
        {
            return new FailedResponseWithMessage("User image not found.");
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var contentType = GetContentType(imagePath);

        return new ImageResponseSuccess(new ImageData(imageBytes, contentType));
    }

    public async Task<ResponseBase> ExamineUserNotExistingAsync(string username, string email)
    {
        var userWithSameEmail = await userManager.FindByEmailAsync(email);
        if (userWithSameEmail != null)
        {
            return new FailedResponseWithMessage("This E-mail address is already taken.");
        }

        var userWithSameUserName = await userManager.FindByEmailAsync(username);
        if (userWithSameEmail != null)
        {
            return new FailedResponseWithMessage("This Username is already taken.");
        }

        return new AuthResponseSuccess();
    }

    public async Task<ResponseBase> GetUserCredentialsAsync(string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId!);

        if (existingUser == null)
        {
            return new FailedResponse();
        }

        return new GetUserCredentialsSuccess(new GetUserCredentialsData(existingUser.UserName!, existingUser.Email!, existingUser.TwoFactorEnabled));
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
            return new FailedResponseWithMessage("Invalid Roomid format.");
        }

        var userGuid = new Guid(userId);
        var existingUser = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .FirstOrDefaultAsync(u => u.Id == userGuid && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

        if (existingUser == null)
        {
            return new FailedResponseWithMessage("Cannot find the key for this user.");
        }

        var userKey = existingUser.UserSymmetricKeys.FirstOrDefault(key => key.RoomId == roomGuid)!.ToString();

        return new KeyResponseSuccess(userKey!);
    }

    public async Task<ResponseBase> GetRoommatePublicKey(string username)
    {
        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage($"There is no user with this Username: {userManager}");
        }

        return new KeyResponseSuccess(existingUser.PublicKey);
    }
    public async Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string username, string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            return new FailedResponseWithMessage("Invalid Roomid format.");
        }

        var userExisting = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .AnyAsync(u => u.UserName == username && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

        if (!userExisting)
        {
            return new FailedResponseWithMessage($"There is no key or user with this Username: {username}");
        }

        return new UserResponseSuccess();
    }
    public async Task<ResponseBase> SendForgotPasswordEmailAsync(string email)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        EmailSenderCodeGenerator.StorePasswordResetCode(email, token);
        var emailResult = await emailSender.SendEmailWithLinkAsync(email, "Password reset", token);

        if (emailResult is FailedResponseWithMessage)
        {
            return new FailedResponseWithMessage("Email service is currently unavailable.");
        }

        return new UserResponseSuccessWithMessage("Successfully sent.");
    }

    public async Task<ResponseBase> SetNewPasswordAfterResetEmailAsync(string resetId, PasswordResetRequest request)
    {
        var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(request.Email, resetId, "passwordReset");
        if (!examine)
        {
            return new FailedResponseWithMessage("Invalid or expired reset code.");
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser!);
        await userManager.ResetPasswordAsync(existingUser!, token, request.NewPassword);

        var affectedRows = await repository.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailedResponseWithMessage("Failed to save new password change.");
        }

        return new AuthResponseSuccess();
    }

    public async Task<ResponseBase> ChangeUserEmailAsync(ChangeEmailRequest request, string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId!);

        if (existingUser == null)
        {
            return new FailedResponseWithMessage($"User not found.");
        }

        if (!existingUser!.TwoFactorEnabled)
        {
            return new FailedResponseWithMessage($"2FA not enabled for user.");
        }

        if (existingUser.Email != request.OldEmail)
        {
            return new FailedResponseWithMessage("E-mail address not valid.");
        }

        if (userManager.Users.Any(user => user.Email == request.NewEmail))
        {
            return new FailedResponseWithMessage("Same e-mail.");
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(existingUser, request.NewEmail);
        await userManager.ChangeEmailAsync(existingUser, request.NewEmail, token);

        existingUser.NormalizedEmail = request.NewEmail.ToUpper();
        var affectedRows = await repository.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailedResponseWithMessage("Failed to save email change.");
        }

        return new UsernameUserEmailResponseSuccess(new UserNameEmailData(existingUser.Email!, existingUser.UserName!));
    }

    public async Task<ResponseBase> ChangeUserPasswordAsync(ChangePasswordRequest request, string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId!);

        if (existingUser == null)
        {
            return new FailedResponseWithMessage($"User not found.");
        }

        var correctPassword = await authService.ExamineLoginCredentialsAsync(existingUser!.UserName!, request.OldPassword);
        if (correctPassword is FailedResponse)
        {
            return new FailedResponseWithMessage("Incorrect credentials.");
        }

        await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);
        var affectedRows = await repository.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailedResponseWithMessage("Failed to save password change.");
        }
        return new UsernameUserEmailResponseSuccess(new UserNameEmailData(existingUser.UserName!, existingUser.Email!));
    }

    public async Task<ResponseBase> ChangeUserAvatarAsync(string userId, string image)
    {

        var existingUser = await userManager.FindByIdAsync(userId!);

        if (existingUser == null)
        {
            return new FailedResponseWithMessage($"User not found.");
        }

        var imageSaveResult = SaveImageLocally(existingUser.UserName!, image);

        return imageSaveResult;
    }

    public async Task<ResponseBase> DeleteUserAsync(string userId, string password)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        var existingUser = await userManager.FindByIdAsync(userId!);
        if (existingUser == null)
        {
            return new FailedResponseWithMessage("User not found.");
        }

        if (!await userManager.CheckPasswordAsync(existingUser, password))
        {
            return new FailedResponseWithMessage("Invalid credentials.");
        }

        var removeFriendsResult = await RemoveFriendConnectionsAsync(existingUser);

        if (removeFriendsResult is FailedResponse)
        {
            await transaction.RollbackAsync();
            return removeFriendsResult;
        }

        var identityResult = await userManager.DeleteAsync(existingUser);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return new FailedResponseWithMessage("Failed to delete user.");
        }

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailedResponseWithMessage("Failed to save changes.");
        }

        await transaction.CommitAsync();
        return new UsernameUserEmailResponseSuccess(new UserNameEmailData(existingUser.UserName!, existingUser.Email!));
    }

    private async Task<ResponseBase> RemoveFriendConnectionsAsync(ApplicationUser existingUser)
    {
        var sentFriendRequests = context.FriendConnections!.Where(fc => fc.SenderId == existingUser.Id);
        var receivedFriendRequests = context.FriendConnections!.Where(fc => fc.ReceiverId == existingUser.Id);

        foreach (var friendRequest in sentFriendRequests)
        {
            var receiver = await userManager.FindByIdAsync(friendRequest.ReceiverId.ToString());
            if (receiver != null)
            {
                await context.Entry(receiver).Collection(u => u.Friends).LoadAsync();
                if (!receiver.Friends.Remove(existingUser))
                {
                    return new FailedResponse();
                }
            }
        }

        foreach (var friendRequest in receivedFriendRequests)
        {
            var sender = await userManager.FindByIdAsync(friendRequest.SenderId.ToString());
            if (sender != null)
            {
                await context.Entry(sender).Collection(u => u.Friends).LoadAsync();
                if (!sender.Friends.Remove(existingUser))
                {
                    return new FailedResponse();
                }
            }
        }

        context.FriendConnections!.RemoveRange(sentFriendRequests);
        context.FriendConnections!.RemoveRange(receivedFriendRequests);
        return new UserResponseSuccess();
    }

    public ResponseBase SaveImageLocally(string usernameFileName, string base64Image)
    {
        var folderPath = configuration["ImageFolderPath"]??Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = usernameFileName + ".png";
        var imagePath = Path.Combine(folderPath, imageName);

        if (base64Image.Length <= 1)
        {
            return new FailedResponseWithMessage("No image provided.");
        }

        try
        {
            base64Image = base64Image.Replace("data:image/png;base64,", "");
            var imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            return new UserResponseSuccessWithMessage(imagePath);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding base64 image: {ex.Message}");
            return new FailedResponseWithMessage("Error decoding base64 image.");
        }
    }

    private string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}