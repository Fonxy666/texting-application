using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Model.Requests.Chat;
using Server.Model.Responses.Chat;
using Server.Services.Chat.RoomService;

namespace Server.Controllers;

[Route("[controller]")]
public class ChatController(IRoomService roomRepository) : ControllerBase
{
    [HttpPost("RegisterRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> RegisterRoom([FromBody]RoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (roomRepository.RoomNameTaken(request.RoomName).Result.Result)
        {
            return BadRequest(new { error = "This room's name already taken." });
        }
        
        var result = await roomRepository.RegisterRoomAsync(request.RoomName, request.Password);
        
        if (result.Success == false)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("JoinRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> LoginRoom([FromBody]RoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        
        
        var result = await roomRepository.LoginRoomAsync(request.RoomName, request.Password);

        if (result.Success == false)
        {
            return BadRequest(new { error = "Invalid login credentials." });
        }

        return Ok(result);
    }
    
    [HttpPost("DeleteRoom"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<RoomResponse>> DeleteRoom([FromBody]RoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var result = await roomRepository.DeleteRoomAsync(request.RoomName, request.Password);

        if (result.Success == false)
        {
            return BadRequest(new { error = "Invalid login credentials." });
        }

        return Ok(result);
    }
}