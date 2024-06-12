using Server.Model;
using Server.Model.Responses.User;

namespace Server.Services.User;

public interface IUserServices
{
    Task<bool> ExistingUser(string id);
    Task<ApplicationUser> GetUserWithSentRequests(string userId);
    Task<ApplicationUser> GetUserWithReceivedRequests(string userId);
    string SaveImageLocally(string userNameFileName, string base64Image);
    string GetContentType(string filePath);
    Task<DeleteUserResponse> DeleteAsync(ApplicationUser user);
}