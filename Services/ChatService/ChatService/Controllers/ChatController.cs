using ChatService.Model.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services.Chat.RoomService;
using Textinger.Shared.Filters;
using Textinger.Shared.Responses;

namespace ChatService.Controllers;

[Route("api/v1/[controller]")]
public class ChatController(
    IRoomService roomService,
    ILogger<ChatController> logger
    ) : ControllerBase
{
    [HttpPost("RegisterRoom")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> RegisterRoom([FromBody]RoomRequest request)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            var result = await roomService.RegisterRoomAsync(request, userId);

            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "User not existing." => NotFound(error),
                    _ => StatusCode(500, "Internal server error.")
                };
            }

            if (result is Failure)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("ExamineIfTheUserIsTheCreator")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<bool>> ExamineCreator([FromQuery]string roomId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            if (!Guid.TryParse(roomId, out var roomGuid))
            {
                return BadRequest(false);
            }

            var result = await roomService.UserIsTheCreatorAsync(userId, roomGuid);

            if (!result)
            {
                return BadRequest(false);
            }
            
            return Ok(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpDelete("DeleteRoom")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> DeleteRoom([FromQuery]string roomId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            if (!Guid.TryParse(roomId, out var roomGuid))
            {
                return BadRequest(false);
            }
            
            var result = await roomService.DeleteRoomAsync(userId, roomGuid);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Room not found." => NotFound(error),
                    "Database error." => StatusCode(500,  "Internal server error."),
                    "You don't have permission to delete this room." => Forbid(),
                    _ => BadRequest(error)
                };
            }
            
            return Ok(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPatch("ChangePasswordForRoom")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> ChangePassword([FromBody]ChangeRoomPassword request) 
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            var result = await roomService.ChangePasswordAsync(request, userId);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Room not found." => NotFound(error),
                    "Database error." => StatusCode(500,  "Internal server error."),
                    "You don't have permission to change this room's password." => Forbid(),
                    _ => BadRequest(error)
                };
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing room for {request.Id}.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("JoinRoom")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> LoginRoom([FromBody]JoinRoomRequest request)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            var loginResult = await roomService.LoginAsync(request, userId);
            if (loginResult is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Room not found" => NotFound(error),
                    _ => BadRequest(error)
                };
            }

            return Ok(loginResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error login into {request.RoomName} room.");
            return StatusCode(500, "Internal server error.");
        }
    }
}
