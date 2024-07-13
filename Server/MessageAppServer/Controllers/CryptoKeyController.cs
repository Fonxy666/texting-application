using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Requests;
using Server.Services.PrivateKey;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<ChatController> logger,
    UserManager<ApplicationUser> userManager
    ) : ControllerBase
{
    [HttpGet("GetPrivateKeyAndIv"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<PrivateKeyResponse>> GetPrivateKeyAndIv()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            var privateKey = await privateKeyService.GetEncryptedKeyByUserIdAsync(userGuid);

            return Ok(new PrivateKeyResponse(privateKey.EncryptedPrivateKey, privateKey.Iv));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }
    
    [HttpGet("GetPrivateUserKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<string>> GetPrivateUserKey([FromQuery]string roomId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userGuid = new Guid(userId!);
            var roomGuid = new Guid(roomId);

            var existingUser = await userManager.Users
                .Include(u => u.UserSymmetricKeys)
                .FirstOrDefaultAsync(u => u.Id == userGuid && u.UserSymmetricKeys.Any(k => k.RoomId == roomGuid));

            if (existingUser == null)
            {
                return NotFound();
            }

            var userKey = existingUser.UserSymmetricKeys.FirstOrDefault(key => key.RoomId == roomGuid);
        
            if (userKey == null)
            {
                return NotFound();
            }

            return Ok(new { encryptedKey = userKey.EncryptedKey });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }
}