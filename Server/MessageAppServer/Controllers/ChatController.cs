using Microsoft.AspNetCore.Mvc;
using Server.Model.Chat;
using Server.Requests;
using Server.Responses;
using Server.Services.Chat;
using Server.Services.Chat.MessageService;

namespace Server.Controllers;

[Route("[controller]")]
public class ChatController(
    IRoomService roomRepository,
    IMessageService messageRepository,
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
    
    [HttpGet("GetMessages/{id}")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.GetLast10Messages(id);

        return Ok(result);
    }
}