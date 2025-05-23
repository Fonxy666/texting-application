using Textinger.Shared.Responses;

namespace UserService.Services.MediaService;

public interface IImageService
{
    ResponseBase SaveImageLocally(string usernameFileName, string base64Image);
    string GetContentType(string filePath);
}