﻿using System.Security.Claims;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Requests;
using Server.Model.Requests.EncryptKey;
using Server.Services.Chat.RoomService;
using Server.Services.PrivateKey;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CryptoKeyController(
    IPrivateKeyService privateKeyService,
    ILogger<ChatController> logger,
    UserManager<ApplicationUser> userManager,
    IRoomService roomService
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

            return Ok(new { encryptedKey = userKey!.EncryptedKey });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }
    
    [HttpPost("SaveEncryptedRoomKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<string>> SaveEncryptedRoomKey([FromBody]StoreRoomKeyRequest data)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userGuid = new Guid(userId!);
            var roomGuid = new Guid(data.RoomId);

            var saveKeyResponse = await roomService.AddNewUserKey(roomGuid, userGuid, data.EncryptedKey);
            if (!saveKeyResponse)
            {
                return BadRequest("Something unusual happened.");
            }
            
            return Ok(new {data.EncryptedKey});
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving the key.");
            return StatusCode(500);
        }
    }
    
    [HttpGet("GetPublicKey"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> GetPublicKey([FromQuery]string userName)
    {
        try
        {
            var existingUser = await userManager.FindByNameAsync(userName);
            if (existingUser == null)
            {
                return BadRequest($"There is no user with this Username: {userManager}");
            }

            return Ok(new {existingUser.PublicKey});
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting public key for {userName}.");
            return StatusCode(500, new { message = $"An error occurred while trying to get public key for user {userName}." });
        }
    }
    
    [HttpGet("ExamineIfUserHaveSymmetricKeyForRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> ExamineIfUserHaveSymmetricKeyForRoom([FromQuery]string userName, string roomId)
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