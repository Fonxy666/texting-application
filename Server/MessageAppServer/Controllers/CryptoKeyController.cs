using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;
using Server.Model.Requests;
using Server.Services.PrivateKey;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<ChatController> logger
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
}