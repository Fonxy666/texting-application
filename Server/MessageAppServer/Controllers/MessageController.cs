using Microsoft.AspNetCore.Mvc;
using Server.Services.Chat.MessageService;
using Microsoft.AspNetCore.Authorization;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Model.Responses.Message;

namespace Server.Controllers;

[Route("[controller]")]
public class MessageController(IMessageService messageRepository) : ControllerBase
{
    [HttpGet("GetMessages/{id}"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<IQueryable<Message>>> GetMessages(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.GetLast10Messages(id);

        return Ok(result);
    }
    
    [HttpPost("SendMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> SendMessage([FromBody]MessageRequest request)
    {
        Console.WriteLine(request.UserName);
        Console.WriteLine(request.Message);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.SendMessage(request);

        if (result.Success == false)
        {
            return BadRequest(new { error = "Invalid login credentials." });
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

        if (result.Success == false)
        {
            return BadRequest(new { error = "Invalid login credentials." });
        }

        return Ok(result);
    }
    
    [HttpDelete("DeleteMessage"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<MessageResponse>> DeleteMessage([FromQuery]string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await messageRepository.DeleteMessage(id);

        if (result.Success == false)
        {
            return BadRequest(new { error = "Invalid login credentials." });
        }

        return Ok(result);
    }
}
