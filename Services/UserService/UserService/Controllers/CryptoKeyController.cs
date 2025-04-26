using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.EncryptedSymmetricKeyService;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.User;
using UserService.Filters;

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
            var userId = (string)HttpContext.Items["UserId"]!;
            if (userId == null)
            {
                return BadRequest(new FailedResponseWithMessage("There is no Userid provided."));
            }

            var privateKeyResponse = await privateKeyService.GetEncryptedKeyByUserIdAsync(userId);

            if (privateKeyResponse is FailedResponse)
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

    [HttpGet("GetPrivateUserKey")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> GetPrivateUserKey([FromQuery] string roomId)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;

            var getKeyResponse = await userService.GetUserPrivatekeyForRoomAsync(userId!, roomId);

            if (getKeyResponse is FailedResponseWithMessage)
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
    public async Task<ActionResult<ResponseBase>> SaveEncryptedRoomKey([FromBody] StoreRoomKeyRequest data)
    {
        try
        {
            var userId = (string)HttpContext.Items["UserId"]!;
            var userGuid = Guid.Parse(userId);
            var roomGuid = new Guid(data.RoomId);

            var newKey = new EncryptedSymmetricKey(userGuid, data.EncryptedKey, roomGuid);

            var result = await keyService.SaveNewKeyAndLinkToUserAsync(newKey);

            if (result is FailedResponse)
            {
                return BadRequest(result);
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

            if (keyResponse is FailedResponseWithMessage)
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
            var keyExisting = await userService.ExamineIfUserHaveSymmetricKeyForRoom(userName, roomId);

            if (keyExisting is FailedResponseWithMessage)
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