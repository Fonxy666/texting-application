using Microsoft.AspNetCore.Mvc;
using Server.Services.Chat.MessageService;
using Microsoft.AspNetCore.Authorization;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Controllers;

[Route("[controller]")]
public class MessageController(IMessageService messageService, RoomsContext roomsContext, ILogger logger) : ControllerBase
{
    [HttpGet("GetMessages/{roomId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string roomId)
    {
        try
        {
            if (!roomsContext.Rooms.Any(room => room.RoomId == roomId))
            {
                return BadRequest($"There is no room with this id: {roomId}");
            }

            var result = await messageService.GetLast10Messages(roomId);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting messages for room: {roomId}");
            return BadRequest($"Error getting messages for room: {roomId}");
        }
    }
    
    [HttpPost("SendMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> SendMessage([FromBody]MessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await messageService.SendMessage(request);

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

        var result = await messageService.EditMessage(request);

        if (!result.Success)
        {
            return new MessageResponse(false, null, null);
        }

        return Ok(result);
    }
    
    [HttpPatch("EditMessageSeen"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> ModifyMessageSeen([FromBody]EditMessageSeenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageService.EditMessageSeen(request);

        return Ok(result);
    }
    
    [HttpDelete("DeleteMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> DeleteMessage([FromQuery]string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        await messageService.DeleteMessage(id);

        return Ok(new MessageResponse(true, "", null));
    }
}
