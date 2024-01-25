using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Database;
using Server.Model;
using Server.Services.Authentication;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IAuthService authenticationService, UsersContext repository, UserManager<ApplicationUser> userManager) : ControllerBase 
{
    [HttpGet("getUser")]
    public async Task<ActionResult<IdentityUser>> GetUser(string username)
    {
        var existingUser = await userManager.FindByNameAsync(username);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        return existingUser!;
    }

    [HttpGet("getProfilePic")]
    public async Task<ActionResult<string>> GetProfilePic(string username)
    {
        var existingUser = await userManager.FindByNameAsync(username);

        if (existingUser == null)
        {
            return NotFound("User not found.");
        }

        return existingUser.ImageUrl;
    }
}