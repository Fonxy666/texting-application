using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;

namespace UserService.Services.User;

public interface IApplicationUserService
{
    Task<bool> ExamineUserExistingWithIdAsync(string userId);
    Task<ResponseBase> GetUsernameAsync(string userId);
    Task<ResponseBase> GetImageWithIdAsync(string userId);
    Task<ResponseBase> ExamineUserNotExistingAsync(string username, string email);
    Task<ResponseBase> GetUserCredentialsAsync(string userId);
    Task<ApplicationUser> GetUserWithSentRequestsAsync(string userId);
    Task<ApplicationUser> GetUserWithReceivedRequestsAsync(string userId);
    Task<ResponseBase> GetUserPrivatekeyForRoomAsync(string userId, string roomId);
    Task<ResponseBase> GetRoommatePublicKey(string username);
    Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string username, string roomId);
    Task<ResponseBase> SendForgotPasswordEmailAsync(string email);
    Task<ResponseBase> SetNewPasswordAfterResetEmailAsync(string resetId, PasswordResetRequest request);
    Task<ResponseBase> ChangeUserEmailAsync(ChangeEmailRequest request, string userId);
    Task<ResponseBase> ChangeUserPasswordAsync(ChangePasswordRequest request, string userId);
    Task<ResponseBase> ChangeUserAvatarAsync(string userId, string image);
    Task<ResponseBase> DeleteUserAsync(string userId, string password);
    ResponseBase SaveImageLocally(string usernameFileName, string base64Image);
}