﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.EmailSender;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    DatabaseContext repository,
    IUserServices userServices,
    ILogger<UserController> logger,
    IEmailSender emailSender,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("GetUsername"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<UsernameResponse>> GetUsername([FromQuery]string userId)
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
            return StatusCode(500);
        }
    }
    
    [HttpGet("GetUserCredentials"), Authorize(Roles = "User, Admin")]
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
            return StatusCode(500);
        }
    }
    
    [HttpGet("SendForgotPasswordToken")]
    public async Task<ActionResult<ForgotPasswordResponse>> SendForgotPasswordEmail([FromQuery]string email)
    {
        try
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
            EmailSenderCodeGenerator.StorePasswordResetCode(email, token);
            await emailSender.SendEmailWithLinkAsync(email, "Password reset", token);

            return new ForgotPasswordResponse(true, "Successfully sent.");
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {email}");
            return StatusCode(500);
        }
    }
    
    [HttpGet("ExaminePasswordResetLink")]
    public async Task<ActionResult<bool>> ExamineResetId([FromQuery]string email, [FromQuery]string resetId)
    {
        try
        {
            var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, resetId, "passwordReset");
            Console.WriteLine(examine);

            if (!examine)
            {
                return BadRequest(examine);
            }

            return Ok(examine);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {email}");
            return StatusCode(500);
        }
    }
    
    [HttpPost("SetNewPassword")]
    public async Task<ActionResult<bool>> SetNewPassword([FromQuery]string resetId, [FromBody]PasswordResetRequest request)
    {
        try
        {
            var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(request.Email, resetId, "passwordReset");
            Console.WriteLine(examine);
            if (!examine)
            {
                return BadRequest(false);
            }
            
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            var token = await userManager.GeneratePasswordResetTokenAsync(existingUser!);
            await userManager.ResetPasswordAsync(existingUser!, token, request.NewPassword);
            
            await repository.SaveChangesAsync();

            return Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {request.Email}");
            return StatusCode(500);
        }
    }

    [HttpGet("GetImage"), Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> GetImageWithId([FromQuery]string userId)
    {
        try
        {
            var existingUser = await userManager.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }
            
            var folderPath = configuration["ImageFolderPath"] ??
                             Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
            
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
            return StatusCode(500);
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
                return NotFound($"2FA not enabled for user: {existingUser.Id}");
            }

            if (userManager.Users.Any(user => user.Email == request.NewEmail))
            {
                return StatusCode(400);
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
            return StatusCode(500);
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

            if (request.Password != request.PasswordRepeat)
            {
                return BadRequest("Passwords do not match.");
            }

            await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);
                
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.Id}");
            return StatusCode(500);
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
            return StatusCode(500);
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
            return StatusCode(500);
        }
    }
}