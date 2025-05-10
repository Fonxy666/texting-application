using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Authentication;
using UserService.Services.EmailSender;
using Textinger.Shared.Responses;
using UserService.Helpers;

namespace UserService.Services.User;

public class ApplicationUserService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    MainDatabaseContext context,
    IEmailSender emailSender,
    IAuthService authService,
    IUserHelper userHelper
    ) : IApplicationUserService
{
    
    
    public async Task<ResponseBase> GetUserNameAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync<ResponseBase>(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
                new SuccessWithDto<UserNameDto>(new UserNameDto(existingUser.UserName!))
            ),
            message => new FailureWithMessage(message)
        );
    }

    public async Task<ResponseBase> GetImageWithIdAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser =>
            {
                var folderPath = configuration["ImageFolderPath"] ?? 
                                 Path.Combine(Directory.GetCurrentDirectory(), "Avatars");

                var imagePath = Path.Combine(folderPath, $"{existingUser.UserName}.png");

                if (!File.Exists(imagePath))
                {
                    return new FailureWithMessage("User image not found.");
                }

                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var contentType = GetContentType(imagePath);

                return new SuccessWithDto<ImageDto>(new ImageDto(imageBytes, contentType));
            }),
            message => new FailureWithMessage(message)
        );
    }

    public async Task<ResponseBase> GetUserCredentialsAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser =>
                new SuccessWithDto<UsernameUserEmailAndTwoFactorEnabledDto>(
                    new UsernameUserEmailAndTwoFactorEnabledDto(existingUser.UserName!, existingUser.Email!,
                        existingUser.TwoFactorEnabled))),
            message => new FailureWithMessage(message)
        );
    }

    public async Task<ResponseBase> GetUserWithSentRequestsAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserIdIncludeSentRequests,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
                new SuccessWithDto<ApplicationUser>(existingUser)
            ),
            message => new FailureWithMessage(message)
        );
    }

    public async Task<ResponseBase> GetUserWithReceivedRequestsAsync(string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserIdIncludeReceivedRequests,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
                new SuccessWithDto<ApplicationUser>(existingUser)
            ),
            message => new FailureWithMessage(message)
        );
    }
    
    public async Task<ResponseBase> GetUserPrivatekeyForRoomAsync(string userId, string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            return new FailureWithMessage("Invalid Roomid format.");
        }

        var userResponse = await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserIdIncludeSymmetricKeys,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
            {
                var userKey = existingUser.UserSymmetricKeys.FirstOrDefault(key => key.RoomId == roomGuid);

                if (userKey != null)
                {
                    return new SuccessWithDto<UserPrivateKeyDto>(
                        new UserPrivateKeyDto(userKey.EncryptedKey)
                    );
                }
                
                return new FailureWithMessage($"No symmetric key found for room: {roomId}");
            }),
            message => new FailureWithMessage(message)
        );
    
        if (userResponse is FailureWithMessage failure)
        {
            return failure;
        }

        return userResponse;
    }

    public async Task<ResponseBase> GetRoommatePublicKey(string userName)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.Username,
            userName,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
                new SuccessWithDto<UserPublicKeyDto>(new UserPublicKeyDto(existingUser.PublicKey)
            )),
            message => new FailureWithMessage(message)
        );
    }
    public async Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string userName, string roomId)
    {
        if (!Guid.TryParse(roomId, out var roomGuid))
        {
            return new FailureWithMessage("Invalid Roomid format.");
        }

        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UsernameExamineSymmetricKeys,
            userName,
            (Func<ApplicationUser, ResponseBase>)(_ =>  new Success()),
            message => new FailureWithMessage(message),
            roomGuid);
    }
    public async Task<ResponseBase> SendForgotPasswordEmailAsync(string email)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserEmail,
            email,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser => 
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                EmailSenderCodeGenerator.StorePasswordResetCode(email, token);
                var emailResult = await emailSender.SendEmailWithLinkAsync(email, "Password reset", token);

                if (emailResult is FailureWithMessage)
                {
                    return new FailureWithMessage("Email service is currently unavailable.");
                }

                return new SuccessWithMessage("Successfully sent.");
            }),
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> SetNewPasswordAfterResetEmailAsync(string resetId, PasswordResetRequest request)
    {
        var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(request.Email, resetId, "passwordReset");
        if (!examine)
        {
            return new FailureWithMessage("Invalid or expired reset code.");
        }

        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserEmail,
            request.Email,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser => 
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetResult = await userManager.ResetPasswordAsync(existingUser, token, request.NewPassword);

                if (!resetResult.Succeeded)
                {
                    return new FailureWithMessage("Failed to save new password change.");
                }

                return new Success();
            }),
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> ChangeUserEmailAsync(ChangeEmailRequest request, string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser => 
            {
                if (!existingUser!.TwoFactorEnabled)
                {
                    return new FailureWithMessage($"2FA not enabled for user.");
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
            }),
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> ChangeUserPasswordAsync(ChangePasswordRequest request, string userId)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser => 
            {
                var correctPassword = await authService.ExamineLoginCredentialsAsync(existingUser!.UserName!, request.OldPassword);
                if (correctPassword is FailureWithMessage)
                {
                    if ((correctPassword as FailureWithMessage)!.Message.Contains("Account is locked"))
                    {
                        await authService.LogOutAsync(userId);
                    }

                    return correctPassword;
                }

                var changeResult = await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);

                if (!changeResult.Succeeded)
                {
                    var errorMessages = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                    return new FailureWithMessage("Failed to change Password.");
                }

                return new SuccessWithDto<UserNameEmailDto>(new UserNameEmailDto(existingUser.UserName!, existingUser.Email!));
            }),
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> ChangeUserAvatarAsync(string userId, string image)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, ResponseBase>)(existingUser => 
            {
                var imageSaveResult = SaveImageLocally(existingUser.UserName!, image);

                return imageSaveResult;
            }),
            message => new FailureWithMessage(message));
    }

    public async Task<ResponseBase> DeleteUserAsync(string userId, string password)
    {
        return await userHelper.GetUserOrFailureResponseAsync(
            UserIdentifierType.UserId,
            userId,
            (Func<ApplicationUser, Task<ResponseBase>>)(async existingUser => 
            {
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

                var affectedRows = await context.SaveChangesAsync();
                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync();
                    return new FailureWithMessage("Failed to save changes.");
                }

                await transaction.CommitAsync();
                return new SuccessWithDto<UserNameEmailDto>(new UserNameEmailDto(existingUser.UserName!, existingUser.Email!));
            }),
            message => new FailureWithMessage(message));
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