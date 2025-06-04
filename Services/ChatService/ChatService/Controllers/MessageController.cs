using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services.Chat.RoomService;
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
    public async Task<ActionResult<ResponseBase>> GetMessages(GetMessagesRequest request)
    {
        try
        {
            var result = await messageService.GetLast10Messages(request);
            if (result is FailureWithMessage)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error getting messages for room: {request.RoomId}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpPost("SendMessage")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody]MessageRequest request)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
        
            var result = await messageService.SendMessage(request, userId);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "Internal server error." => StatusCode(500, "Internal server error."),
                    _ => NotFound(error)
                };
            }
            
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending message for room: {request.RoomId}");
            return StatusCode(500);
        }
    }
    
    [HttpPatch("EditMessage")]
    [Authorize(Roles = "User, Admin")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ChatMessageResponse>> EditMessage([FromBody]EditMessageRequest request)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            var result = await messageService.EditMessage(request, userId);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "You don't have permission." => Forbid(),
                    "There is no message with the given id." => NotFound(error),
                    _ => StatusCode(500, "Internal server error.")
                };
            }

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
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            var result = await messageService.EditMessageSeen(request, userId);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "There is no message with the given id." => NotFound(error),
                    _ => StatusCode(500, "Internal server error.")
                };
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error editing message seen list with id: {request.MessageId}");
            return StatusCode(500);
        }
    }
    
    [HttpDelete("DeleteMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ChatMessageResponse>> DeleteMessage([FromQuery]Guid messageId)
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            
            var result = await messageService.DeleteMessage(messageId, userId);
            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    "There is no message with the given id." => NotFound(error),
                    "You don't have permission." => Forbid(),
                    _ => StatusCode(500, "Internal server error.")
                };
            }

            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error deleting message with id: {messageId}");
            return StatusCode(500);
        }
    }
}
