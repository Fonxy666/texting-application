using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.EmailSender;
using Textinger.Shared.Responses;

namespace UserService.Services.User;

public class ApplicationUserService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    MainDatabaseContext context,
    IEmailSender emailSender,
    IAuthService authService
    ) : IApplicationUserService
{
    
    public async Task<ResponseBase> GetUserNameAsync(Guid userId)
    {
        var userNameDto = await context.Users
            .Where(u => u.Id ==  userId)
            .Select(u => 
                new UserNameDto(u.UserName!))
            .FirstOrDefaultAsync();

        if (userNameDto is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        return new SuccessWithDto<UserNameDto>(userNameDto);
    }

    public async Task<ResponseBase> GetImageWithIdAsync(Guid userId)
    {
        var userName = await context.Users
            .Where(u => u.Id ==  userId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync();

        if (userName is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var folderPath = configuration["ImageFolderPath"] ?? 
                        Path.Combine(Directory.GetCurrentDirectory(), "Avatars");

        var imagePath = Path.Combine(folderPath, $"{userName}.png");

        if (!File.Exists(imagePath))
        {
            return new FailureWithMessage("User image not found.");
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var contentType = GetContentType(imagePath);

        return new SuccessWithDto<ImageDto>(new ImageDto(imageBytes, contentType));
    }

    public async Task<ResponseBase> GetUserCredentialsAsync(Guid userId)
    {
        var userCredentials = await context.Users
            .Where(u => u.Id ==  userId)
            .Select(u => 
                new UsernameUserEmailAndTwoFactorEnabledDto(u.UserName!, u.Email!, u.TwoFactorEnabled))
            .FirstOrDefaultAsync();

        if (userCredentials is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        return new SuccessWithDto<UsernameUserEmailAndTwoFactorEnabledDto>(userCredentials);
    }

    public async Task<ResponseBase> GetUserWithSentRequestsAsync(Guid userId)
    {
        var existingUser = await context.Users
            .Include(u => u.SentFriendRequests)
            .Where(u => u.Id ==  userId)
            .FirstOrDefaultAsync();
        
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }

        return new SuccessWithDto<ApplicationUser>(existingUser);
    }

    public async Task<ResponseBase> GetUserWithReceivedRequestsAsync(Guid userId)
    {
        var existingUser = await context.Users
            .Include(u => u.ReceivedFriendRequests)
            .Where(u => u.Id ==  userId)
            .FirstOrDefaultAsync();
        
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }

        return new SuccessWithDto<ApplicationUser>(existingUser);
    }
    
    public async Task<ResponseBase> GetUserPrivatekeyForRoomAsync(Guid userId, Guid roomId)
    {
        var userKeyDto = await context.Users
            .Include(u => u.UserSymmetricKeys)
            .Where(u => u.Id == userId &&
                        u.UserSymmetricKeys.Any(esk => esk.RoomId == roomId))
            .Select(u => new UserPrivateKeyDto(
                u.UserSymmetricKeys.First(k => k.RoomId == roomId).EncryptedKey))
            .FirstOrDefaultAsync();
        
        if (userKeyDto is null)
        {
            return new FailureWithMessage("User with the desired key not found.");
        }

        return new SuccessWithDto<UserPrivateKeyDto>(userKeyDto);
    }

    public async Task<ResponseBase> GetRoommatePublicKey(string userName)
    {
        var publicKey = await context.Users
            .Where(u => u.UserName ==  userName)
            .Select(u => new UserPublicKeyDto(u.PublicKey))
            .FirstOrDefaultAsync();
        
        return new SuccessWithDto<UserPublicKeyDto>(publicKey);
    }
    public async Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string userName, Guid roomId)
    {
        var result = await context.Users
            .Include(u => u.UserSymmetricKeys)
            .AnyAsync(u => u.UserName == userName && u.UserSymmetricKeys.Any(esk => esk.RoomId == roomId));

        if (!result)
        {
            return new FailureWithMessage("The user don't have the key.");
        }

        return new Success();
    }
    public async Task<ResponseBase> SendForgotPasswordEmailAsync(string email)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        Console.WriteLine(token);
        EmailSenderCodeGenerator.StorePasswordResetCode(email, token);
        var emailResult = await emailSender.SendEmailWithLinkAsync(email, "Password reset", token);

        if (emailResult is FailureWithMessage)
        {
            return new FailureWithMessage("Email service is currently unavailable.");
        }

        return new SuccessWithMessage("Successfully sent.");
    }

    public async Task<ResponseBase> SetNewPasswordAfterResetEmailAsync(string resetId, PasswordResetRequest request)
    {
        var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(request.Email, resetId, EmailType.PasswordReset);
        if (!examine)
        {
            return new FailureWithMessage("Invalid or expired reset code.");
        }
        
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }

        var resetResult = await userManager.ResetPasswordAsync(existingUser, resetId, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            return new FailureWithMessage("Failed to save new password change.");
        }

        return new Success();
    }

    public async Task<ResponseBase> ChangeUserEmailAsync(ChangeEmailRequest request, Guid userId)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        } 

        if (existingUser.Email != request.OldEmail)
        {
            return new FailureWithMessage("E-mail address not valid.");
        }

        if (userManager.Users.Any(user => user.Email == request.NewEmail))
        {
            return new FailureWithMessage("This email is already in use.");
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(existingUser, request.NewEmail);
        var changeResult = await userManager.ChangeEmailAsync(existingUser, request.NewEmail, token);

        if (!changeResult.Succeeded)
        {
            return new FailureWithMessage("Failed to change Email.");
        }

        return new SuccessWithDto<UserEmailDto>(new UserEmailDto( request.NewEmail ));
    }

    public async Task<ResponseBase> ChangeUserPasswordAsync(ChangePasswordRequest request, Guid userId)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var correctPassword = await authService.ExamineLoginCredentialsAsync(existingUser!.UserName!, request.OldPassword);
        if (correctPassword is FailureWithMessage error)
        {
            if (error.Message.Contains("Account is locked"))
            {
                await authService.LogOutAsync(userId);
            }

            return error;
        }

        var changeResult = await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);

        if (!changeResult.Succeeded)
        {
            var errorMessages = string.Join(", ", changeResult.Errors.Select(e => e.Description));
            return new FailureWithMessage("Failed to change Password.");
        }

        return new SuccessWithDto<UserNameEmailDto>(new UserNameEmailDto(existingUser.UserName!, existingUser.Email!));
    }

    public async Task<ResponseBase> ChangeUserAvatarAsync(Guid userId, string image)
    {
        var userName = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync();
        if (userName is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var imageSaveResult = SaveImageLocally(userName, image);

        return imageSaveResult;
    }

    public async Task<ResponseBase> DeleteUserAsync(Guid userId, string password)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        await using var transaction = await context.Database.BeginTransactionAsync();

        if (!await userManager.CheckPasswordAsync(existingUser, password))
        {
            return new FailureWithMessage("Invalid credentials.");
        }

        var removeFriendsResult = await RemoveFriendConnectionsAsync(existingUser);

        if (removeFriendsResult is Failure)
        {
            await transaction.RollbackAsync();
            return removeFriendsResult;
        }

        var identityResult = await userManager.DeleteAsync(existingUser);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return new FailureWithMessage("Failed to delete user.");
        }

        await transaction.CommitAsync();
        return new SuccessWithDto<UserNameEmailDto>(new UserNameEmailDto(existingUser.UserName!, existingUser.Email!));
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
                    return new Failure();
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
                    return new Failure();
                }
            }
        }

        context.FriendConnections!.RemoveRange(sentFriendRequests);
        context.FriendConnections!.RemoveRange(receivedFriendRequests);
        return new Success();
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
            return new FailureWithMessage("No image provided.");
        }

        try
        {
            base64Image = base64Image.Replace("data:image/png;base64,", "");
            var imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            return new SuccessWithMessage(imagePath);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding base64 image: {ex.Message}");
            return new FailureWithMessage("Error decoding base64 image.");
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