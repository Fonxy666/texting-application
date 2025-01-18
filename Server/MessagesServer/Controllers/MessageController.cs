using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MessagesServer.Model.Chat;
using MessagesServer.Model.Requests.Message;
using MessagesServer.Model.Responses.Message;
using MessagesServer.Services.Chat.MessageService;
using MessagesServer.Services.Chat.RoomService;

namespace MessagesServer.Controllers;

[Route("api/v1/[controller]")]
public class MessageController(
    IMessageService messageService,
    IRoomService roomService,
    ILogger<MessageController> logger
    ) : ControllerBase
{
    [HttpGet("GetMessages/{roomId}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string roomId)
    {
        try
        {
            var parsedRoomId = new Guid(roomId);
            if (!roomService.ExistingRoom(parsedRoomId).Result)
            {
                return NotFound($"There is no room with this id: {roomId}");
            }

            var result = await messageService.GetLast10Messages(parsedRoomId);

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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var roomIdToGuid = new Guid(request.RoomId);
            if (!roomService.ExistingRoom(roomIdToGuid).Result)
            {
                return NotFound($"There is no room with this id: {request.RoomId}");
            }
        
            var result = await messageService.SendMessage(request, userId!);
        
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending message for room: {request.RoomId}");
            return StatusCode(500);
        }
    }
    
    [HttpPatch("EditMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> EditMessage([FromBody]EditMessageRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);

            if (!await messageService.MessageExisting(request.Id))
            {
                return NotFound($"There is no message with this given id: {request.Id}");
            }
            
            if (!await messageService.UserIsTheSender(userGuid, request.Id))
            {
                return BadRequest("You are not supposed to edit other user's messages.");
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
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);
            
            var result = await messageService.EditMessageSeen(request, userGuid);

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userGuid = new Guid(userId!);
            var idToGuid = new Guid(id);
            if (!messageService.MessageExisting(idToGuid).Result)
            {
                return NotFound($"There is no message with this given id: {id}");
            }

            if (!await messageService.UserIsTheSender(userGuid, idToGuid))
            {
                return BadRequest("You are not allowed to delete other users messages.");
            }
            
            await messageService.DeleteMessage(idToGuid);

            return Ok(new MessageResponse(true, "", null));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting message with id: {id}");
            return StatusCode(500);
        }
    }
}
