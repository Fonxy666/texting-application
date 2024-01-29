using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Database;
using Server.Model;
using Server.Services.Authentication;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IAuthService authenticationService, UsersContext repository, UserManager<ApplicationUser> userManager) : ControllerBase 
{
    [HttpGet("getUserEmail")]
    public async Task<ActionResult<UserResponse>> GetUserEmail(string username)
    {
        var existingUser = await userManager.FindByNameAsync(username);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        var response = new UserResponse { Email = existingUser.Email };

        return response;
    }

    [HttpGet("GetImage/{imageName}")]
    public IActionResult GetImage(string imageName)
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        var imagePath = Path.Combine(folderPath, $"{imageName}.png");
        FileContentResult result = null;

        if (System.IO.File.Exists(imagePath))
        {
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);

            var contentType = GetContentType(imagePath);

            result = File(imageBytes, contentType);
        }
        
        return result ?? (IActionResult)NotFound();
    }

    private string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}