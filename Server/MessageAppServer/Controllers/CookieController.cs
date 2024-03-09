using Microsoft.AspNetCore.Mvc;
using Server.Services.Cookie;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CookieController(ICookieService cookieService) : ControllerBase
{
    [HttpPost("ChangeCookie")]
    public Task<ActionResult<bool>> ChangeAnimateOrAnonymousCookie([FromQuery]string request)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<ActionResult<bool>>(BadRequest(ModelState));
        }

        if (request == "Animation")
        {
            cookieService.ChangeAnimation();
        }
        else if (request == "Anonymous")
        {
            cookieService.ChangeUserAnonymous();
        }

        return Task.FromResult<ActionResult<bool>>(Ok(true));
    }
}