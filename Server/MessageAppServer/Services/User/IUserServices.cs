using Server.Model;
using Server.Model.Responses.User;

namespace Server.Services.User;

public interface IUserServices
{
    string SaveImageLocally(string userNameFileName, string base64Image);
    string GetContentType(string filePath);
    Task<DeleteUserResponse> DeleteAsync(ApplicationUser user);
}