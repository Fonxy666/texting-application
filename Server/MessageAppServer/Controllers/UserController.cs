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
    IUserServices userServices) : ControllerBase 
{
    [HttpGet("getUsername/{userId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UsernameResponse>> GetUsername(string userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
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
            
            Response.Headers.Add("Cache-Control", "max-age=3600, public");

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
            
            Response.Headers.Add("Cache-Control", "max-age=3600, public");

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
        var existingUser = await userManager.FindByEmailAsync(request.OldEmail);
        if (existingUser == null)
        {
            return BadRequest(ModelState);
        }

        if (!existingUser.TwoFactorEnabled)
        {
            return BadRequest(ModelState);
        }
            
        var token = await userManager.GenerateChangeEmailTokenAsync(existingUser, request.NewEmail);
        await userManager.ChangeEmailAsync(existingUser, request.NewEmail, token);
            
        existingUser.NormalizedEmail = request.NewEmail.ToUpper();
        await repository.SaveChangesAsync();
        var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
        return Ok(response);
    }
    
    [HttpPatch("ChangePassword"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> ChangeUserPassword([FromBody]ChangeUserPasswordRequest request)
    {
        var existingUser = await userManager.FindByIdAsync(request.Id);
        if (existingUser == null)
        {
            return BadRequest(ModelState);
        }

        await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);

        await repository.SaveChangesAsync();
            
        await repository.SaveChangesAsync();
        var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
        return Ok(response);
    }

    [HttpPatch("ChangeAvatar"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<AuthResponse>> ChangeAvatar([FromBody]AvatarChange request)
    {
        var existingUser = await userManager.FindByIdAsync(request.UserId);
        if (existingUser == null)
        {
            return BadRequest(ModelState);
        }

        userServices.SaveImageLocally(existingUser.UserName!, request.Image);
        return Ok(new { Status = "Ok" });
    } 
    
    [HttpDelete("DeleteUser"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> DeleteUser([FromQuery]string email, [FromQuery]string username, [FromQuery]string password)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            return BadRequest(ModelState);
        }

        await authenticationService.DeleteAsync(username, password);
        
        await repository.SaveChangesAsync();
        var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
        return Ok(response);
    }
}