using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Database;
using Server.Model;
using Server.Requests;
using Server.Responses;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    UsersContext repository,
    ILogger<AuthController> logger) : ControllerBase 
{
    [HttpGet("getUserCredentials")]
    public async Task<ActionResult<UserResponse>> GetUserEmail(string username)
    {
        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        var response = new UserResponse(existingUser.Email, existingUser.TwoFactorEnabled);

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
    
    [HttpPatch("ChangeEmail")]
    public async Task<ActionResult<ChangeEmailRequest>> ChangeUserEmail([FromBody] ChangeEmailRequest request)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.OldEmail);
            Console.WriteLine(existingUser);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {request.OldEmail} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }
            
            var result = await userManager.ChangeEmailAsync(existingUser, request.NewEmail, request.Token);

            await repository.SaveChangesAsync();

            if (result.Succeeded)
            {
                await repository.SaveChangesAsync();
                var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
                return Ok(response);
            }
            else
            {
                logger.LogError($"Error changing e-mail for user {request.OldEmail}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing e-mail for user {request.OldEmail}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing e-mail for user {request.OldEmail}");
            return NotFound($"Error changing e-mail for user {request.OldEmail}");
        }
    }
    
    [HttpPatch("ChangePassword")]
    public async Task<ActionResult<EmailUsernameResponse>> ChangeUserPassword([FromBody] ChangeUserPasswordRequest request)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {request.Email} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }

            var result = await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.NewPassword);

            await repository.SaveChangesAsync();

            if (result.Succeeded)
            {
                await repository.SaveChangesAsync();
                var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
                return Ok(response);
            }
            else
            {
                logger.LogError($"Error changing password for user {request.Email}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing password for user {request.Email}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.Email}");
            return NotFound($"Error changing password for user {request.Email}");
        }
    }
}