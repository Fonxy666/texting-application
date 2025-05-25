using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.FriendConnectionService;
using UserService.Services.User;
using UserService.Services.EmailSender;
using UserService.Models.Responses;
using UserService.Filters;
using UserService.Models.Requests;
using UserService.Services.Authentication;
using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UserController(
    ILogger<UserController> logger,
    IFriendConnectionService friendConnectionService,
    IApplicationUserService userService,
    IAuthService authenticationService
    ) : ControllerBase
{
    [HttpGet("GetUsername")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetUsername([FromQuery]string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid user id from get username."));
            }
            var userNameResponse = await userService.GetUserNameAsync(userGuid);

            if (userNameResponse is FailureWithMessage)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var userResponse = await userService.GetUserCredentialsAsync(userId);

            if (userResponse is Failure)
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

            if (emailResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error),
                    "Email service is currently unavailable." => StatusCode(500, error)
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
            var decodedToken = resetId.Replace(" ", "+");
            var examine = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, decodedToken, EmailType.PasswordReset);

            if (!examine)
            {
                return BadRequest(new Failure());
            }

            return Ok(new Success());
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
            var decodedToken = resetId.Replace(" ", "+");
            var newPasswordResult = await userService.SetNewPasswordAfterResetEmailAsync(decodedToken, request);

            if (newPasswordResult is FailureWithMessage)
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
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid user id from get image."));
            }
            
            var getImageResult = await userService.GetImageWithIdAsync(userGuid);

            if (getImageResult is FailureWithMessage)
            {
                return NotFound(getImageResult);
            }

            var image = getImageResult as SuccessWithDto<ImageDto>;

            Response.Headers.Append("Cache-Control", "max-age=1, public");
            return File(image!.Data!.ImageBytes, image!.Data!.ContentType);
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var changeEmailResponse = await userService.ChangeUserEmailAsync(request, userId);

            if (changeEmailResponse is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error),
                    _ => BadRequest(error)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var changePasswordResult = await userService.ChangeUserPasswordAsync(request, userId);

            if (changePasswordResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error),
                    _ => BadRequest(error)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;
            var imageSaveResult = await userService.ChangeUserAvatarAsync(userId, image);

            if (imageSaveResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"User not found." => NotFound(error),
                    _ => BadRequest(error)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            await authenticationService.LogOutAsync(userId!);

            var deleteResult = await userService.DeleteUserAsync(userId, password);

            if (deleteResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == "User not found." => NotFound(error),
                    _ => BadRequest(error)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var sendFriendRequestResult = await friendConnectionService.SendFriendRequestAsync(userId, friendName);

            if (sendFriendRequestResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"There is no User with this username: {friendName}" => NotFound(error),
                    "You cannot send friend request to yourself." => BadRequest(error),
                    "Failed to save changes." => StatusCode(500, error),
                    _ => BadRequest(error)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var result = await friendConnectionService.GetPendingRequestCountAsync(userId!);

            if (result is FailureWithMessage)
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
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var requests = await friendConnectionService.GetAllPendingRequestsAsync(userId);

            if (requests is FailureWithMessage)
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
    
    [HttpPatch("AcceptReceivedFriendRequest")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult> AcceptFriendRequest([FromBody]string requestId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            if (!Guid.TryParse(requestId, out var requestGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid room ID format."));
            }

            var acceptFriendRequestResult = await friendConnectionService.AcceptReceivedFriendRequestAsync(userId, requestGuid);

            if (acceptFriendRequestResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Request not found." => NotFound(error),
                    "Invalid request ID format." => BadRequest(error),
                    _ => StatusCode(500, error)
                };
            }

            return Ok(acceptFriendRequestResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error accepting friend request.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpDelete("DeleteFriendRequest")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult> DeleteFriendRequest([FromQuery]string requestId, string userType)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            UserType? parsedUserType = userType switch
            {
                "receiver" => UserType.Receiver,
                "sender" => UserType.Sender,
                _ => null
            };
            
            if (parsedUserType is null)
            {
                return BadRequest(new FailureWithMessage("Invalid user type."));
            }
            
            if (!Guid.TryParse(requestId, out var requestGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid room ID format."));
            }

            var deleteFriendRequestResult = await friendConnectionService.DeleteFriendRequestAsync(userId, parsedUserType.Value, requestGuid);

            if (deleteFriendRequestResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Cannot find the request." => NotFound(error),
                    "Invalid request ID format." => BadRequest(error),
                    _ => StatusCode(500, error)
                };
            }

            return Ok(deleteFriendRequestResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error declining friend request.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("GetFriends")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult> GetFriends()
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var getFriendsResult = await friendConnectionService.GetFriendsAsync(userId);

            if (getFriendsResult is FailureWithMessage)
            {
                return NotFound(getFriendsResult);
            }

            return Ok(getFriendsResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting friends.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpDelete("DeleteFriend")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult> DeleteFriend([FromQuery]string requestId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            if (!Guid.TryParse(requestId, out var requestGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid request ID format."));
            }
            
            var friendDeletionResult = await friendConnectionService.DeleteFriendAsync(userId, requestGuid);

            if (friendDeletionResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Cannot find friend connection." => NotFound(error),
                    "Invalid connectionId format." => BadRequest(error),
                    "You don't have permission for deletion." => Unauthorized(error),
                    _ => StatusCode(500, error)
                };
            }


            return Ok(friendDeletionResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting friend.");
            return StatusCode(500, "Internal server error.");
        }
    }
}