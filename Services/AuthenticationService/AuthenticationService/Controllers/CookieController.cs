using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthenticationService.Services.Cookie;

namespace AuthenticationService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CookieController(ICookieService cookieService, ILogger<CookieController> logger) : ControllerBase
{
    [HttpPost("ChangeCookies"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult> ChangeAnimateOrAnonymousCookie([FromQuery]string request)
    {
        try
        {
            if (request is not ("Animation" or "Anonymous"))
            {
                return BadRequest(false);
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

            return Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing {request} cookie.");
            return StatusCode(500);
        }
    }
}