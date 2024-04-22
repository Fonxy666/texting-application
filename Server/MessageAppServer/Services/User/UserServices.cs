using Microsoft.AspNetCore.Http.HttpResults;

namespace Server.Services.User;

public class UserServices : IUserServices
{
    public string SaveImageLocally(string userNameFileName, string base64Image)
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
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
}