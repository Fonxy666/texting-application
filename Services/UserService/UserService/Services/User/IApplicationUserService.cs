using UserService.Models.Requests;
using Textinger.Shared.Responses;

namespace UserService.Services.User;

public interface IApplicationUserService
{
    Task<ResponseBase> GetUserNameAsync(Guid userId);
    Task<ResponseBase> GetImageWithIdAsync(Guid userId);
    Task<ResponseBase> GetUserCredentialsAsync(Guid userId);
    Task<ResponseBase> GetUserWithSentRequestsAsync(Guid userId);
    Task<ResponseBase> GetUserWithReceivedRequestsAsync(Guid userId);
    Task<ResponseBase> GetUserPrivatekeyForRoomAsync(Guid userId, Guid roomId);
    Task<ResponseBase> GetRoommatePublicKey(string userName);
    Task<ResponseBase> ExamineIfUserHaveSymmetricKeyForRoom(string userName, Guid roomId);
    Task<ResponseBase> SendForgotPasswordEmailAsync(string email);
    Task<ResponseBase> SetNewPasswordAfterResetEmailAsync(string resetId, PasswordResetRequest request);
    Task<ResponseBase> ChangeUserEmailAsync(ChangeEmailRequest request, Guid userId);
    Task<ResponseBase> ChangeUserPasswordAsync(ChangePasswordRequest request, Guid userId);
    Task<ResponseBase> ChangeUserAvatarAsync(Guid userId, string image);
    Task<ResponseBase> DeleteUserAsync(Guid userId, string password);
    ResponseBase SaveImageLocally(string userNameFileName, string base64Image);
}