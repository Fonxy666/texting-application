using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.FriendConnectionService;
using UserService.Services.User;
using UserService.Services.EmailSender;
using UserService.Models.Responses;
using UserService.Filters;
using UserService.Models.Requests;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController(
    ILogger<UserController> logger,
    IFriendConnectionService friendConnectionService,
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
    
    [HttpPatch("ChangePassword")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> ChangeUserPassword([FromBody]ChangePasswordRequest request)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var changePasswordResult = await userService.ChangeUserPasswordAsync(request, userId);

            if (changePasswordResult is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error.Message),
                    _ => BadRequest(error.Message)
                };
            }

            return Ok(changePasswordResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error changing password for user.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPatch("ChangeAvatar")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> ChangeAvatar([FromBody]string image)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;
            var imageSaveResult = await userService.ChangeUserAvatarAsync(userId, image);

            if (imageSaveResult is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error.Message),
                    _ => BadRequest(error.Message)
                };
            }

            return Ok(imageSaveResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error changing avatar for user.");
            return StatusCode(500, "Internal server error.");
        }
    } 
    
    [HttpDelete("DeleteUser")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> DeleteUser([FromQuery]string password)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var deleteResult = await userService.DeleteUserAsync(userId, password);

            if (deleteResult is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == "User not found." => NotFound(error.Message),
                    _ => BadRequest(error.Message)
                };
            }

            return Ok(deleteResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting the user.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("SendFriendRequest")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> SendFriendRequest([FromBody]string friendName)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var sendFriendRequestResult = await friendConnectionService.SendFriendRequestAsync(userId, friendName);

            if (sendFriendRequestResult is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == "User not found." => NotFound(error.Message),
                    "New friend not found." => NotFound(error.Message),
                    _ => BadRequest(error.Message)
                };
            }

            return Ok(sendFriendRequestResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending friend request.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("GetFriendRequestCount")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetFriendRequestCount()
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var result = await friendConnectionService.GetPendingRequestCountAsync(userId!);

            if (result is FailedResponseWithMessage)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting the number of friend requests.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("GetFriendRequests")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetFriendRequests()
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var requests = await friendConnectionService.GetAllPendingRequestsAsync(userId);

            if (requests is FailedResponseWithMessage)
            {
                return NotFound(requests);
            }

            return Ok(requests);
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