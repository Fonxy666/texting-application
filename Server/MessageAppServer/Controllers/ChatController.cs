using Microsoft.AspNetCore.Mvc;
using Server.Contracts;
using Server.Services.Chat;

namespace Server.Controllers;

[Route("[controller]")]
public class ChatController(
    IRoomService roomRepository,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("RegisterRoom")]
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

    [HttpPost("JoinRoom")]
    public async Task<ActionResult<RoomResponse>> LoginRoom([FromBody] RoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var result = await roomRepository.LoginRoomAsync(request.RoomName, request.Password);
        if (result.Success == false)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}