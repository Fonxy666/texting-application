using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services.Chat.RoomService;
using ChatService.Model;
using ChatService.Model.Requests;
using ChatService.Model.Responses.Message;
using ChatService.Services.Chat.MessageService;
using Textinger.Shared.Filters;
using Textinger.Shared.Responses;

namespace ChatService.Controllers;

[Route("api/v1/[controller]")]
public class MessageController(
    IMessageService messageService,
    IRoomService roomService,
    ILogger<MessageController> logger
    ) : ControllerBase
{
    [HttpGet("GetMessages/{roomId}")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(GetMessagesRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.RoomId, out var roomGuid))
            {
                return BadRequest(new Failure());
            }

            var result = await messageService.GetLast10Messages(roomGuid, request.Index);
            if (result is FailureWithMessage)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting messages for room: {request.RoomId}");
            return StatusCode(500);
        }
    }
    
    [HttpPost("SendMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody]MessageRequest request)
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
    public async Task<ActionResult<ChatMessageResponse>> EditMessage([FromBody]EditMessageRequest request)
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
    public async Task<ActionResult<ChatMessageResponse>> ModifyMessageSeen([FromBody]EditMessageSeenRequest request)
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
    public async Task<ActionResult<ChatMessageResponse>> DeleteMessage([FromQuery]string id)
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

            return Ok(new ChatMessageResponse(true, "", null));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting message with id: {id}");
            return StatusCode(500);
        }
    }
}
