using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    UsersContext repository,
    IUserServices userServices,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("getUsername/{userId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UsernameResponse>> GetUsername(string userId)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            var response = new UsernameResponse(existingUser.UserName!);

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting username for user {userId}");
            return BadRequest($"Error getting username for user {userId}");
        }
    }
    
    [HttpGet("getUserCredentials"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UserResponse>> GetUserEmail([FromQuery]string userId)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            var response = new UserResponse(existingUser.UserName, existingUser.Email, existingUser.TwoFactorEnabled);

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting e-mail for user {userId}");
            return BadRequest($"Error getting e-mail for user {userId}");
        }
    }

    [HttpGet("GetImage/{userId}")]
    public async Task<IActionResult> GetImageWithId(string userId)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }
            
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            var imagePath = Path.Combine(folderPath, $"{existingUser!.UserName}.png");
            FileContentResult result = null;

            if (System.IO.File.Exists(imagePath))
            {
                var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                var contentType = userServices.GetContentType(imagePath);
                
                Response.Headers.Add("Cache-Control", "max-age=3600, public");

                result = File(imageBytes, contentType);
            }
            
            return result ?? (IActionResult)NotFound();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting avatar image for user {userId}");
            return BadRequest($"Error getting avatar image for user {userId}");
        }
    }
    
    [HttpGet("GetImageWithUsername/{userName}")]
    public async Task<IActionResult> GetImageWithUsername(string userName)
    {
        try
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            var imagePath = Path.Combine(folderPath, $"{userName}.png");
            FileContentResult result = null;

            if (System.IO.File.Exists(imagePath))
            {
                var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                var contentType = userServices.GetContentType(imagePath);
                
                Response.Headers.Add("Cache-Control", "max-age=3600, public");

                result = File(imageBytes, contentType);
            }
            
            return result ?? (IActionResult)NotFound();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting avatar image for user {userName}");
            return BadRequest($"Error getting avatar image for user {userName}");
        }
    }
    
    [HttpPatch("ChangeEmail"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ChangeEmailRequest>> ChangeUserEmail([FromBody]ChangeEmailRequest request)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.OldEmail);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            if (!existingUser.TwoFactorEnabled)
            {
                return BadRequest($"2FA not enabled for user: {existingUser.Id}");
            }
                
            var token = await userManager.GenerateChangeEmailTokenAsync(existingUser, request.NewEmail);
            await userManager.ChangeEmailAsync(existingUser, request.NewEmail, token);
                
            existingUser.NormalizedEmail = request.NewEmail.ToUpper();
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing e-mail for user {request.OldEmail}");
            return BadRequest($"Error changing password for user {request.OldEmail}");
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
                return NotFound("User not found.");
            }

            await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);

            await repository.SaveChangesAsync();
                
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.Id}");
            return BadRequest($"Error changing password for user {request.Id}");
        }
    }

    [HttpPatch("ChangeAvatar"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<AuthResponse>> ChangeAvatar([FromBody]AvatarChange request)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(request.UserId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            userServices.SaveImageLocally(existingUser.UserName!, request.Image);
            return Ok(new { Status = "Ok" });
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing avatar for user {request.UserId}");
            return BadRequest($"Error changing avatar for user {request.UserId}");
        }
    } 
    
    [HttpDelete("DeleteUser"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> DeleteUser([FromQuery]string email, [FromQuery]string password)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            if (!userManager.CheckPasswordAsync(existingUser, password).Result)
            {
                return BadRequest("Invalid credentials.");
            }

            await userServices.DeleteAsync(existingUser);
            
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing e-mail for user {email}");
            return BadRequest($"Error changing e-mail for user {email}");
        }
    }
}