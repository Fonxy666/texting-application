using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Model.Requests.Chat;
using ChatService.Services.Chat.RoomService;
using ChatService.Model.Responses.Chat;
using ChatService.Services.Chat.GrpcService;
using ChatService.Model.Requests.EncryptKey;

namespace ChatService.Controllers;

[Route("api/v1/[controller]")]
public class ChatController(
    IRoomService roomService,
    ILogger<ChatController> logger,
    IUserGrpcService userGrpcService
    ) : ControllerBase
{
    [HttpPost("RegisterRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> RegisterRoom([FromBody] RoomRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest(new { error = "Cannot get user id from credentials." });
            }
            var existingUser = await userGrpcService.UserExisting(userId);
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "New room credentials not valid." });
            }

            if (roomService.RoomNameTaken(request.RoomName).Result.Result)
            {
                return BadRequest(new { error = "This room's name already taken." });
            }

            var result = await roomService.RegisterRoomAsync(request.RoomName, request.Password, new Guid(userId), request.EncryptedSymmetricRoomKey);

            if (result.Success == false)
            {
                return BadRequest(new { error = "Something failed during room creation in the database." });
            }

            var sendUserUpdateInfos = await userGrpcService.SendEncryptedRoomIdForUser(
                new StoreRoomKeyRequest(
                    new Guid(userId),
                    request.EncryptedSymmetricRoomKey,
                    new Guid(result.RoomId)
                    )
                );

            if (!sendUserUpdateInfos.Success)
            {
                Console.WriteLine(sendUserUpdateInfos);
                return BadRequest(new { error = "There was an error communicating with the grpc server." });
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }

    [HttpGet("ExamineIfTheUserIsTheCreator"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<bool>> ExamineCreator([FromQuery] string roomId)
    {
        try
        {
            var existingRoom = await roomService.GetRoomById(new Guid(roomId));
            if (existingRoom == null)
            {
                return NotFound(false);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            if (!existingRoom.IsCreator(userGuid))
            {
                return Ok(false);
            }

            return Ok(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpDelete("DeleteRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<bool>> DeleteRoom([FromQuery] string roomId)
    {
        try
        {
            var guidRoomId = new Guid(roomId);

            var existingRoom = await roomService.GetRoomById(guidRoomId);
            if (existingRoom == null)
            {
                return NotFound(false);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            if (!existingRoom.IsCreator(userGuid))
            {
                return BadRequest(false);
            }

            await roomService.DeleteRoomAsync(existingRoom);

            return Ok(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPatch("ChangePasswordForRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> ChangePassword([FromBody] ChangeRoomPassword request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var existingRoom = await roomService.GetRoomById(new Guid(request.Id));

            if (existingRoom == null)
            {
                return NotFound(new { error = "There is no room with the given Room id." });
            }

            if (!existingRoom.PasswordMatch(request.OldPassword))
            {
                return BadRequest("Incorrect old password credentials.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            if (!existingRoom.IsCreator(userGuid))
            {
                return BadRequest(false);
            }

            await roomService.ChangePassword(existingRoom, request.Password);

            return Ok(new RoomResponse(true, existingRoom.RoomId.ToString(), existingRoom.RoomName));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing room for {request.Id}.");
            return StatusCode(500);
        }
    }

    [HttpPost("JoinRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> LoginRoom([FromBody] JoinRoomRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var existingRoom = await roomService.GetRoomByRoomName(request.RoomName);

            if (existingRoom == null)
            {
                return NotFound(new { error = "There is no room with the given Room name." });
            }

            if (!existingRoom.PasswordMatch(request.Password))
            {
                return BadRequest("Incorrect login credentials");
            }

            return Ok(new RoomResponse(true, existingRoom.RoomId.ToString(), existingRoom.RoomName));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error login into {request.RoomName} room.");
            return StatusCode(500);
        }
    }
}