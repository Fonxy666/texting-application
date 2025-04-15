using UserService.Model;
using UserService.Model.Responses;

namespace UserService.Services.User;

public interface IApplicationUserService
{
    Task<bool> ExamineUserExistingWithIdAsync(string id);
    Task<ResponseBase> ExamineUserNotExistingAsync(string Username, string Email);
    Task<ApplicationUser> GetUserWithSentRequestsAsync(string userId);
    Task<ApplicationUser> GetUserWithReceivedRequestsAsync(string userId);
    Task<ResponseBase> GetUserPrivatekeyForRoomAsync(string userId, string roomId);
    string SaveImageLocally(string userNameFileName, string base64Image);
    string GetContentType(string filePath);
    Task<DeleteUserResponse> DeleteAsync(ApplicationUser user);
}