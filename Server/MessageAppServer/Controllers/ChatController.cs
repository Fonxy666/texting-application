using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Server.Contracts;
using Server.Services.Chat;

namespace Server.Controllers;

public class ChatController(
    IRoomService roomRepository,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("Chat/RegisterRoom")]
    public async Task<ActionResult<CreateRoomResponse>> RegisterRoom([FromBody]CreateRoomRequest request)
    {
        Console.WriteLine(request);
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
}