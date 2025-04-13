using UserService.Model;

namespace UserService.Services.User;

public interface IApplicationUserService
{
    Task<bool> ExistingUser(string id);
    Task<ApplicationUser> GetUserWithSentRequests(string userId);
    Task<ApplicationUser> GetUserWithReceivedRequests(string userId);
    string SaveImageLocally(string userNameFileName, string base64Image);
    string GetContentType(string filePath);
    Task<DeleteUserResponse> DeleteAsync(ApplicationUser user);
}