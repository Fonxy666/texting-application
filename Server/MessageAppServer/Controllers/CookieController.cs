using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services.Cookie;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CookieController(ICookieService cookieService, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("ChangeCookies"), Authorize(Roles = "User, Admin")]
    public Task<ActionResult<bool>> ChangeAnimateOrAnonymousCookie([FromQuery]string request)
    {
        switch (request)
        {
            case "Animation":
                cookieService.ChangeAnimation();
                break;
            case "Anonymous":
                cookieService.ChangeUserAnonymous();
                break;
        }

        return Task.FromResult<ActionResult<bool>>(Ok(true));
    }
}