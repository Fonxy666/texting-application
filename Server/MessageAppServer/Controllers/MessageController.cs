using Microsoft.AspNetCore.Mvc;
using Server.Services.Chat.MessageService;
using Microsoft.AspNetCore.Authorization;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Controllers;

[Route("[controller]")]
public class MessageController(IMessageService messageRepository, RoomsContext roomsContext) : ControllerBase
{
    [HttpGet("GetMessages/{roomId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string roomId)
    {
        if (!roomsContext.Rooms.Any(room => room.RoomId == roomId))
        {
            return BadRequest($"There is no room with this id: {roomId}");
        }

        var result = await messageRepository.GetLast10Messages(roomId);

        return Ok(result);
    }
    
    [HttpPost("SendMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> SendMessage([FromBody]MessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await messageRepository.SendMessage(request);

        if (!result.Success)
        {
            return BadRequest(result.errorMessage);
        }
        
        return Ok(result);
    }
    
    [HttpPatch("EditMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> ModifyMessage([FromBody]EditMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.EditMessage(request);

        return Ok(result);
    }
    
    [HttpPatch("EditMessageSeen"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> ModifyMessageSeen([FromBody]EditMessageSeenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.EditMessageSeen(request);

        return Ok(result);
    }
    
    [HttpDelete("DeleteMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> DeleteMessage([FromQuery]string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        await messageRepository.DeleteMessage(id);

        return Ok(new MessageResponse(true, ""));
    }
}
