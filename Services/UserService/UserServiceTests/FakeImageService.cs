using Textinger.Shared.Responses;
using UserService.Models;
using UserService.Services.EmailSender;
using UserService.Services.MediaService;

namespace UserServiceTests;

public class FakeImageService : IImageService
{
    public ResponseBase SaveImageLocally(string usernameFileName, string base64Image)
    {
        return new SuccessWithMessage("fake/image/path.jpg");
    }

    public string GetContentType(string filePath)
    {
        return "fake/image/path.jpg";
    }
}