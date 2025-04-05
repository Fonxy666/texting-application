using UserService.Model;
using UserService.Model.Responses.User;

namespace UserService.Services.User;

public interface IUserServices
{
    Task<bool> ExistingUser(string id);
    Task<ApplicationUser> GetUserWithSentRequests(string userId);
    Task<ApplicationUser> GetUserWithReceivedRequests(string userId);
    string SaveImageLocally(string userNameFileName, string base64Image);
    string GetContentType(string filePath);
    Task<DeleteUserResponse> DeleteAsync(ApplicationUser user);
    Task AddNewKeyAsync(Guid userId, Guid roomGuid, EncryptedSymmetricKey newKey);
    Task AddNewRoomToUser(Guid userId, Guid roomGuid, EncryptedSymmetricKey newKey);
}