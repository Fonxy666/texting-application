using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.FriendConnection;
using UserService.Services.User;
using UserService.Services.EmailSender;
using UserService.Services.Authentication;
using UserService.Models.Responses;
using UserService.Filters;
using UserService.Models.Requests;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController(
    IAuthService authenticationService,
    MainDatabaseContext repository,
    IApplicationUserService userServices,
    ILogger<UserController> logger,
    IFriendConnectionService friendConnectionService,
    IConfiguration configuration,
    IApplicationUserService userService
    ) : ControllerBase
{
    [HttpGet("GetUsername")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetUsername([FromQuery]string userId)
    {
        try
        {
            var userNameResponse = await userService.GetUsernameAsync(userId);

            if (userNameResponse is FailedResponseWithMessage)
            {
                return NotFound(userNameResponse);
            }

            return Ok(userNameResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting username for user {userId}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("GetUserCredentials")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetUserCredentials()
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var userResponse = await userService.GetUserCredentialsAsync(userId);

            if (userResponse is FailedResponse)
            {
                return BadRequest(userResponse);
            }

            return Ok(userResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting e-mail for user.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("SendForgotPasswordToken")]
    public async Task<ActionResult<ResponseBase>> SendForgotPasswordEmail([FromQuery]string email)
    {
        try
        {
            var emailResult = await userService.SendForgotPasswordEmailAsync(email);

            if (emailResult is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error.Message),
                    "Email service is currently unavailable." => StatusCode(500, error.Message)
                };
            }

            return Ok(emailResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {email}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("ExaminePasswordResetLink")]
    public ActionResult<ResponseBase> ExamineResetId([FromQuery]string email, [FromQuery]string resetId)
    {
        try
        {
            var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, resetId, "passwordReset");

            if (!examine)
            {
                return BadRequest(new FailedResponse());
            }

            return Ok(new AuthResponseSuccess());
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {email}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpPost("SetNewPassword")]
    public async Task<ActionResult<ResponseBase>> SetNewPassword([FromQuery]string resetId, [FromBody]PasswordResetRequest request)
    {
        try
        {
            var newPasswordResult = await userService.SetNewPasswordAfterResetEmailAsync(resetId, request);

            if (newPasswordResult is FailedResponseWithMessage)
            {
                return BadRequest(newPasswordResult);
            }

            return Ok(newPasswordResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error reset password for user {request.Email}");
            return StatusCode(500, "Internal server error.");
        }
    }

    [ExcludeFromCodeCoverage]
    [HttpGet("GetImage")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<IActionResult> GetImageWithId([FromQuery]string userId)
    {
        try
        {
            var getImageResult = await userService.GetImageWithIdAsync(userId);

            if (getImageResult is FailedResponseWithMessage)
            {
                return NotFound(getImageResult);
            }

            Response.Headers.Append("Cache-Control", "max-age=1, public");
            return Ok(getImageResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting avatar image for user {userId}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpPatch("ChangeEmail")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> ChangeUserEmail([FromBody]ChangeEmailRequest request)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var changeEmailResponse = await userService.ChangeUserEmailAsync(request, userId);

            if (changeEmailResponse is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error.Message),
                    _ => BadRequest(error.Message)
                };
            }

            return Ok(changeEmailResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing e-mail for user {request.OldEmail}");
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
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
            return StatusCode(500, "Internal server error.");
        }
    }
}