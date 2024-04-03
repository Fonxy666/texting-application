using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.Authentication;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    UsersContext repository,
    IAuthService authenticationService,
    IUserServices userServices,
    ILogger<AuthController> logger) : ControllerBase 
{
    [HttpGet("getUsername/{UserId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UsernameResponse>> GetUsername(string UserId)
    {
        var existingUser = await userManager.FindByIdAsync(UserId);
        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        var response = new UsernameResponse(existingUser.UserName!);

        return response;
    }
    
    [HttpGet("getUserCredentials"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UserResponse>> GetUserEmail([FromQuery]string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        var response = new UserResponse(existingUser.UserName, existingUser.Email, existingUser.TwoFactorEnabled);

        return response;
    }

    [HttpGet("GetImage/{userId}")]
    public async Task<IActionResult> GetImageWithId(string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        var imagePath = Path.Combine(folderPath, $"{existingUser!.UserName}.png");
        FileContentResult result = null;

        if (System.IO.File.Exists(imagePath))
        {
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);

            var contentType = GetContentType(imagePath);

            result = File(imageBytes, contentType);
        }
        
        return result ?? (IActionResult)NotFound();
    }
    
    [HttpGet("GetImageWithUsername/{userName}")]
    public async Task<IActionResult> GetImageWithUsername(string userName)
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        var imagePath = Path.Combine(folderPath, $"{userName}.png");
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
    
    [HttpPatch("ChangeEmail"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ChangeEmailRequest>> ChangeUserEmail([FromBody]ChangeEmailRequest request)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.OldEmail);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {request.OldEmail} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }

            if (!existingUser.TwoFactorEnabled)
            {
                return BadRequest(ModelState);
            }
            
            var token = await userManager.GenerateChangeEmailTokenAsync(existingUser, request.NewEmail);
            var result = await userManager.ChangeEmailAsync(existingUser, request.NewEmail, token);

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
    
    [HttpPatch("ChangePassword"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> ChangeUserPassword([FromBody]ChangeUserPasswordRequest request)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(request.Id);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for id: {request.Id} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }

            var result = await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);

            await repository.SaveChangesAsync();

            if (result.Succeeded)
            {
                await repository.SaveChangesAsync();
                var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
                return Ok(response);
            }
            else
            {
                logger.LogError($"Error changing password for user {request.Id}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing password for user {request.Id}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.Id}");
            return NotFound($"Error changing password for user {request.Id}");
        }
    }

    [HttpPost("ChangeAvatar"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<AuthResponse>> ChangeAvatar([FromBody]AvatarChange request)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(request.UserId);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for id: {request.UserId} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }

            userServices.SaveImageLocally(existingUser.UserName!, request.Image);
            return Ok(new { Status = "Ok" });
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.UserId}");
            return NotFound($"Error changing password for user {request.UserId}");
        }
    } 
    
    [HttpDelete("DeleteUser"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> DeleteUser([FromQuery]string email, [FromQuery]string username, [FromQuery]string password)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {email} doesn't exist in the database.");
                return BadRequest(ModelState);
            }

            var result = await authenticationService.DeleteAsync(username, password);

            if (!result.Successful)
            {
                logger.LogError($"Error deleting user {email}: {string.Join(", ", result)}");
                return BadRequest($"Error deleting user {email}");
            }
        
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting user {email}");
            return NotFound($"Error deleting user {email}");
        }
    }
}