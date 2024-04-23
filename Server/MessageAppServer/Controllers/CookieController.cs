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
        try
        {
            if (request is not ("Animation" or "Anonymous"))
            {
                return Task.FromResult<ActionResult<bool>>(BadRequest(false));
            }
            
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
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing {request} cookie.");
            return Task.FromResult<ActionResult<bool>>(BadRequest($"Error changing {request} cookie."));
        }
    }
}