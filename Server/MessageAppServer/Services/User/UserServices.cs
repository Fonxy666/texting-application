using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Responses.User;

namespace Server.Services.User;

public class UserServices(UserManager<ApplicationUser> userManager, IConfiguration configuration) : IUserServices
{
    public Task<bool> ExistingUser(string id)
    {
        return userManager.Users.AnyAsync(user => user.Id.ToString() == id);
    }

    public string SaveImageLocally(string userNameFileName, string base64Image)
    {
        var folderPath = configuration["ImageFolderPath"]??Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = userNameFileName + ".png";
        var imagePath = Path.Combine(folderPath, imageName);

        try
        {
            if (base64Image.Length <= 1)
            {
                return "";
            }
            
            base64Image = base64Image.Replace("data:image/png;base64,", "");
            var imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            return imagePath;
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding base64 image: {ex.Message}");
            throw;
        }
    }

    public string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    public async Task<DeleteUserResponse> DeleteAsync(ApplicationUser user)
    {
        await userManager.DeleteAsync(user);

        return new DeleteUserResponse($"{user.UserName}", "Delete successful.", true);
    }
}