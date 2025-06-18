using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.EncryptedSymmetricKeyService;
using UserService.Models.Requests;
using UserService.Services.User;
using Textinger.Shared.Filters;
using Textinger.Shared.Responses;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<CryptoKeyController> logger,
    ISymmetricKeyService keyService,
    IApplicationUserService userService
    ) : ControllerBase
{
    [HttpGet("GetPrivateKeyAndIv")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetPrivateKeyAndIv()
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var privateKeyResponse = await privateKeyService.GetEncryptedKeyByUserIdAsync(userId);
    
            if (privateKeyResponse is Failure)
            {
                return BadRequest(privateKeyResponse);
            }

            return Ok(privateKeyResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("GetPrivateUserKey/{roomId:guid}")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetPrivateUserKey(Guid roomId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;

            var getKeyResponse = await userService.GetUserPrivatekeyForRoomAsync(userId, roomId);

            if (getKeyResponse is FailureWithMessage)
            {
                return BadRequest(getKeyResponse);
            }

            return Ok(getKeyResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("SaveEncryptedRoomKey")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> SaveEncryptedRoomKey([FromBody]StoreRoomKeyRequest data)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            if (!Guid.TryParse(data.RoomId, out var roomGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid room ID format."));
            }

            var newKey = new EncryptedSymmetricKey(userId, data.EncryptedKey, roomGuid);

            var result = await keyService.SaveNewKeyAndLinkToUserAsync(newKey);

            if (result is Failure)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving the key.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("GetPublicKey")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetPublicKey([FromQuery] string userName)
    {
        try
        {
            var keyResponse = await userService.GetRoommatePublicKey(userName);

            if (keyResponse is FailureWithMessage)
            {
                return BadRequest(keyResponse);
            }

            return Ok(keyResponse);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting public key for {userName}.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("ExamineIfUserHaveSymmetricKeyForRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> ExamineIfUserHaveSymmetricKeyForRoom([FromQuery] string userName, string roomId)
    {
        try
        {
            if (!Guid.TryParse(roomId, out var roomGuid))
            {
                return BadRequest(new FailureWithMessage("Invalid room ID format."));
            }
            var keyExisting = await userService.ExamineIfUserHaveSymmetricKeyForRoom(userName, roomGuid);

            if (keyExisting is FailureWithMessage)
            {
                return BadRequest(keyExisting);
            }

            return Ok(keyExisting);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting public key for {userName}.");
            return StatusCode(500, "Internal server error.");
        }
    }
}