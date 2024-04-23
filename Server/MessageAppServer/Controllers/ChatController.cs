using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Model.Requests.Chat;
using Server.Model.Responses.Chat;
using Server.Services.Chat.RoomService;

namespace Server.Controllers;

[Route("[controller]")]
public class ChatController(IRoomService roomService, ILogger logger) : ControllerBase
{
    [HttpPost("RegisterRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> RegisterRoom([FromBody]RoomRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "New room credentials not valid." });
            }

            if (roomService.RoomNameTaken(request.RoomName).Result.Result)
            {
                return BadRequest(new { error = "This room's name already taken." });
            }
            
            var result = await roomService.RegisterRoomAsync(request.RoomName, request.Password);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return BadRequest("Error registering the room");
        }
    }

    [HttpPost("JoinRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> LoginRoom([FromBody]RoomRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Room credentials not valid." });
            }

            var existingRoom = await roomService.GetRoom(request.RoomName);

            if (existingRoom == null)
            {
                return NotFound(new { error = "There is no room with the given Room name." });
            }

            if (existingRoom.PasswordMatch(existingRoom.Password))
            {
                return NotFound(new { error = "Invalid login credentials." });
            }
            
            return Ok(new RoomResponse(true, existingRoom.RoomId, existingRoom.RoomName));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error login into {request.RoomName} room.");
            return BadRequest($"Error login into {request.RoomName} room.");
        }
    }
    
    [HttpPost("DeleteRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> DeleteRoom([FromBody]RoomRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var existingRoom = roomService.GetRoom(request.RoomName).Result;

            if (existingRoom == null)
            {
                return NotFound(new { error = "There is no room with the given Room name." });
            }
            
            if (existingRoom.PasswordMatch(existingRoom.Password))
            {
                return NotFound(new { error = "Invalid room credentials." });
            }
            
            await roomService.DeleteRoomAsync(existingRoom);

            return Ok(new RoomResponse(true, existingRoom.RoomId, existingRoom.RoomName));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting room {request.RoomName}.");
            return BadRequest($"Error deleting room {request.RoomName}.");
        }
    }
}