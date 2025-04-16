using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.EncryptedSymmetricKeyService;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<CryptoKeyController> logger,
    UserManager<ApplicationUser> userManager,
    ISymmetricKeyService keyService,
    IApplicationUserService userService
    ) : ControllerBase
{
    [HttpGet("GetPrivateKeyAndIv"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ResponseBase>> GetPrivateKeyAndIv()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest(new FailedUserResponseWithMessage("There is no Userid provided."));
            }

            var privateKeyResponse = await privateKeyService.GetEncryptedKeyByUserIdAsync(userId);

            if (privateKeyResponse is FailedUserResponse)
            {
                return BadRequest(privateKeyResponse);
            }

            return Ok(privateKeyResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }

    [HttpGet("GetPrivateUserKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ResponseBase>> GetPrivateUserKey([FromQuery] string roomId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest(new FailedUserResponseWithMessage("There is no Userid provided."));
            }

            var getKeyResponse = await userService.GetUserPrivatekeyForRoomAsync(userId!, roomId);

            if (getKeyResponse is FailedUserResponseWithMessage error)
            {
                return BadRequest(new FailedUserResponse());
            }

            return Ok(getKeyResponse as PrivateKeyResponseSuccess);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }

    [HttpPost("SaveEncryptedRoomKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<string>> SaveEncryptedRoomKey([FromBody] StoreRoomKeyRequest data)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userGuid = new Guid(userId!);
            var roomGuid = new Guid(data.RoomId);

            var newKey = new EncryptedSymmetricKey(userGuid, data.EncryptedKey, roomGuid);

            var result = await keyService.SaveNewKeyAndLinkToUserAsync(newKey);

            if (result == null)
            {
                return BadRequest("Error saving the new key");
            }

            return Ok(new { data.EncryptedKey });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving the key.");
            return StatusCode(500);
        }
    }

    [HttpGet("GetPublicKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> GetPublicKey([FromQuery] string userName)
    {
        try
        {
            var existingUser = await userManager.FindByNameAsync(userName);
            if (existingUser == null)
            {
                return BadRequest($"There is no user with this Username: {userManager}");
            }

            return Ok(new { existingUser.PublicKey });
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting public key for {userName}.");
            return StatusCode(500, new { message = $"An error occurred while trying to get public key for user {userName}." });
        }
    }

    [HttpGet("ExamineIfUserHaveSymmetricKeyForRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> ExamineIfUserHaveSymmetricKeyForRoom([FromQuery] string userName, string roomId)
    {
        try
        {
            var roomGuid = new Guid(roomId);
            var existingUser = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

            if (existingUser == null)
            {
                return BadRequest($"There is no key or user with this Username: {userName}");
            }

            return Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting public key for {userName}.");
            return StatusCode(500, new { message = $"An error occurred while trying to get public key for user {userName}." });
        }
    }
}