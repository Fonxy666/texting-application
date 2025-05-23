using Microsoft.AspNetCore.StaticFiles;
using Textinger.Shared.Responses;

namespace UserService.Services.MediaService;

public class ImageService(IConfiguration configuration) : IImageService
{
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

    public string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}