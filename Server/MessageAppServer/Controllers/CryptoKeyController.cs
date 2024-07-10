using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;
using Server.Services.PrivateKey;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<ChatController> logger
    ) : ControllerBase
{
    [HttpGet("GetPrivateKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<string>> RegisterRoom()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            var encryptedPrivateKey = await privateKeyService.GetEncryptedKeyByUserIdAsync(userGuid);

            return Ok(new { key = encryptedPrivateKey });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }
}