using Microsoft.AspNetCore.Mvc;
using Server.Services.Chat.MessageService;
using Microsoft.AspNetCore.Authorization;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;
using Server.Services.Chat.RoomService;
using Server.Services.User;

namespace Server.Controllers;

[Route("api/v1/[controller]")]
public class MessageController(
    IMessageService messageService,
    IRoomService roomService,
    ILogger<MessageController> logger,
    IUserServices userServices
    ) : ControllerBase
{
    [HttpGet("GetMessages/{roomId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string roomId)
    {
        try
        {
            if (!roomService.ExistingRoom(roomId).Result)
            {
                return NotFound($"There is no room with this id: {roomId}");
            }

            var result = await messageService.GetLast10Messages(roomId);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting messages for room: {roomId}");
            return StatusCode(500);
        }
    }
    
    [HttpPost("SendMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> SendMessage([FromBody]MessageRequest request)
    {
        Console.WriteLine(request);
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (!roomService.ExistingRoom(request.RoomId).Result)
            {
                return NotFound($"There is no room with this id: {request.RoomId}");
            }

            if (!userServices.ExistingUser(request.UserId).Result)
            {
                return NotFound($"There is no user with this id: {request.UserId}");
            }
        
            var result = await messageService.SendMessage(request);
        
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending message for room: {request.RoomId}");
            return StatusCode(500);
        }
    }
    
    [HttpPatch("EditMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> ModifyMessage([FromBody]EditMessageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!messageService.MessageExisting(request.Id).Result)
            {
                return NotFound($"There is no message with this given id: {request.Id}");
            }
            
            var result = await messageService.EditMessage(request);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error editing message with id: {request.Id}");
            return StatusCode(500);
        }
    }
    
    [HttpPatch("EditMessageSeen"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> ModifyMessageSeen([FromBody]EditMessageSeenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (!messageService.MessageExisting(request.MessageId).Result)
            {
                return NotFound($"There is no message with this given id: {request.MessageId}");
            }

            var result = await messageService.EditMessageSeen(request);

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error editing message seen list with id: {request.MessageId}");
            return StatusCode(500);
        }
    }
    
    [HttpDelete("DeleteMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> DeleteMessage([FromQuery]string id)
    {
        try
        {
            if (!messageService.MessageExisting(id).Result)
            {
                return NotFound($"There is no message with this given id: {id}");
            }
            
            await messageService.DeleteMessage(id);

            return Ok(new MessageResponse(true, "", null));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting message with id: {id}");
            return StatusCode(500);
        }
    }
}
