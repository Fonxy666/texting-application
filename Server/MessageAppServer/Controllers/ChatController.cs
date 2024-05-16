using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Requests;
using Server.Model.Requests.Chat;
using Server.Model.Requests.User;
using Server.Model.Responses.Chat;
using Server.Services.Chat.MessageService;
using Server.Services.Chat.RoomService;

namespace Server.Controllers;

[Route("api/v1/[controller]")]
public class ChatController(IRoomService roomService, ILogger<ChatController> logger, UserManager<ApplicationUser> userManager, IMessageService messageService) : ControllerBase
{
    [HttpPost("RegisterRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> RegisterRoom([FromBody]RoomRequest request)
    {
        Console.WriteLine(request.RoomName);
        Console.WriteLine(request.Password);
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
            
            var result = await roomService.RegisterRoomAsync(request.RoomName, request.Password, new Guid(request.CreatorId));

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering the room");
            return StatusCode(500);
        }
    }

    [HttpGet("ExamineIfTheUserIsTheCreator"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<bool>> ExamineCreator([FromQuery]string userId, [FromQuery]string roomId)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(false);
            }

            var existingRoom = await roomService.GetRoomById(new Guid(roomId));
            if (existingRoom == null)
            {
                return NotFound(false);
            }

            var existingUser = await userManager.Users.FirstOrDefaultAsync(user => user.Id == new Guid(userId));
            if (existingUser == null)
            {
                return NotFound(false);
            }

            if (!existingRoom.IsCreator(existingUser.Id))
            {
                return BadRequest(false);
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
    public async Task<ActionResult<bool>> DeleteRoom([FromQuery]string userId, [FromQuery]string roomId)
    {
        try
        {
            var guidRoomId = new Guid(roomId);
            if (!ModelState.IsValid)
            {
                return BadRequest(false);
            }

            var existingRoom = await roomService.GetRoomById(guidRoomId);
            if (existingRoom == null)
            {
                return NotFound(false);
            }

            var existingUser = await userManager.Users.FirstOrDefaultAsync(user => user.Id == new Guid(userId));
            if (existingUser == null)
            {
                return NotFound(false);
            }

            if (!existingRoom.IsCreator(existingUser.Id))
            {
                return BadRequest(false);
            }

            await messageService.DeleteMessages(guidRoomId);
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
    public async Task<ActionResult<RoomResponse>> ChangePassword([FromBody]ChangeRoomPassword request)
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
    public async Task<ActionResult<RoomResponse>> LoginRoom([FromBody]JoinRoomRequest request)
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
    
    [HttpPost("DeleteRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> DeleteRoom([FromBody]RoomRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var existingRoom = roomService.GetRoomByRoomName(request.RoomName).Result;

            if (existingRoom == null)
            {
                return NotFound(new { error = "There is no room with the given Room name." });
            }
            
            if (!existingRoom.PasswordMatch(request.Password))
            {
                return BadRequest("Incorrect credentials");
            }
            
            await roomService.DeleteRoomAsync(existingRoom);

            return Ok(new RoomResponse(true, existingRoom.RoomId.ToString(), existingRoom.RoomName));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting room {request.RoomName}.");
            return StatusCode(500);
        }
    }
}