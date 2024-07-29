using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Database;
using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.Authentication;
using Server.Services.EmailSender;
using Server.Services.FriendConnection;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    IAuthService authenticationService,
    MainDatabaseContext repository,
    IUserServices userServices,
    ILogger<UserController> logger,
    IEmailSender emailSender,
    IFriendConnectionService friendConnectionService,
    IConfiguration configuration
    ) : ControllerBase
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
    public async Task<ActionResult<UserResponse>> GetUserCredentials()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingUser = await userManager.FindByIdAsync(userId!);

            var response = new UserResponse(existingUser.UserName, existingUser.Email, existingUser.TwoFactorEnabled);

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting e-mail for user.");
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

    [ExcludeFromCodeCoverage]
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
                
                Response.Headers.Add("Cache-Control", "max-age=1, public");

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingUser = await userManager.FindByIdAsync(userId!);

            if (!existingUser!.TwoFactorEnabled)
            {
                return NotFound($"2FA not enabled for user: {existingUser.Id}");
            }

            if (existingUser.Email != request.OldEmail)
            {
                return BadRequest("E-mail address not valid.");
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
    public async Task<ActionResult<EmailUsernameResponse>> ChangeUserPassword([FromBody]ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingUser = await userManager.FindByIdAsync(userId!);
            
            var correctPassword = await authenticationService.ExamineLoginCredentials(existingUser!.UserName!, request.OldPassword);
            if (!correctPassword.Success)
            {
                return BadRequest("Incorrect credentials.");
            }

            await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.Password);
            await repository.SaveChangesAsync();
            var response = new EmailUsernameResponse(existingUser.Email!, existingUser.UserName!);
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user.");
            return StatusCode(500);
        }
    }

    [HttpPatch("ChangeAvatar"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<AuthResponse>> ChangeAvatar([FromBody]string image)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingUser = await userManager.FindByIdAsync(userId!);

            userServices.SaveImageLocally(existingUser!.UserName!, image);
            return Ok(new { Status = "Ok" });
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing avatar for user.");
            return StatusCode(500);
        }
    } 
    
    [HttpDelete("DeleteUser"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<EmailUsernameResponse>> DeleteUser([FromQuery]string password)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingUser = await userManager.FindByIdAsync(userId!);

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
            logger.LogError(e, $"Error changing e-mail for user.");
            return StatusCode(500);
        }
    }

    [HttpPost("SendFriendRequest"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ShowFriendRequestResponse>> SendFriendRequest([FromBody]string friendName)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingSender = await userManager.FindByIdAsync(userId!);
            var existingReceiver = await userManager.FindByNameAsync(friendName);
            
            if (existingSender == existingReceiver)
            {
                return BadRequest(new { message = "You cannot send friend request to yourself." });
            }
        
            var databaseRequest = new FriendRequest(userId!, existingReceiver!.Id.ToString());
        
            var alreadySent = await friendConnectionService.AlreadySentFriendRequest(databaseRequest);
            if (alreadySent)
            {
                return BadRequest(new { message = "You already sent a friend request to this user!" });
            }

            var result = await friendConnectionService.SendFriendRequest(databaseRequest);
        
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending friend request.");
            return StatusCode(500, new { message = "An error occurred while sending the friend request." });
        }
    }

    [HttpGet("GetFriendRequestCount"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> GetFriendRequestCount()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await friendConnectionService.GetPendingRequestCount(userId!);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to get friend requests count." });
        }
    }
    
    [HttpGet("GetFriendRequests"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> GetFriendRequests()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var receivedFriendRequests = await friendConnectionService.GetPendingReceivedFriendRequests(userId!);
            var sentFriendRequests = await friendConnectionService.GetPendingSentFriendRequests(userId!);

            var allFriendRequests = receivedFriendRequests.Concat(sentFriendRequests).ToList();

            return Ok(allFriendRequests);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to get friend requests." });
        }
    }
    
    [HttpPatch("AcceptReceivedFriendRequest"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> AcceptFriendRequest([FromBody]string requestId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var existingRequest = await friendConnectionService.GetFriendRequestByIdAsync(requestId);

            if (existingRequest == null)
            {
                return NotFound(new { message = "Friend request not found." });
            }

            await friendConnectionService.AcceptReceivedFriendRequest(requestId, userId!);

            return Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error accepting friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to accept the friend request." });
        }
    }
    
    [HttpDelete("DeleteFriendRequest"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> DeleteFriendRequest([FromQuery]string requestId, string userType)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingRequest = await friendConnectionService.GetFriendRequestByIdAsync(requestId);

            if (existingRequest == null)
            {
                return NotFound(new { message = "Friend request not found." });
            }
            
            switch (userType)
            {
                case "receiver":
                    await friendConnectionService.DeleteReceivedFriendRequest(requestId, userId!);
                    break;
                case "sender":
                    await friendConnectionService.DeleteSentFriendRequest(requestId, userId!);
                    break;
            }


            return Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error declining friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to decline the friend request." });
        }
    }
    
    [HttpGet("GetFriends"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> GetFriends()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await friendConnectionService.GetFriends(userId!);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to get friend request." });
        }
    }
    
    [HttpDelete("DeleteFriend"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> DeleteFriend([FromQuery]string connectionId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingFriendConnection = await friendConnectionService.GetFriendRequestByIdAsync(connectionId);

            if (existingFriendConnection == null)
            {
                return NotFound(new { message = "Friend connection not found." });
            }
            
            var userGuid = new Guid(userId!);
            
            if (userGuid != existingFriendConnection.SenderId && userGuid != existingFriendConnection.ReceiverId)
            {
                return BadRequest(new { message = "You don't have permission for deletion." });
            }

            var result = await friendConnectionService.DeleteFriend(connectionId);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending friend request.");
            return StatusCode(500, new { message = "An error occurred while trying to get friend requests." });
        }
    }
}