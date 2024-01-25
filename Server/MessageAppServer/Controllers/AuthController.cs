using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Contracts;
using Server.Database;
using Server.Services.Authentication;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService authenticationService, UsersContext repository, ILogger<AuthController> logger, UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPost("Register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.RegisterAsync(request.Email, request.Username, request.Password, "User");

        if (!result.Success)
        {
            AddErrors(result);
            return BadRequest(ModelState);
        }

        return CreatedAtAction(nameof(Register), new RegistrationResponse(result.Email, result.UserName));
    }

    private void AddErrors(AuthResult result)
    {
        foreach (var resultErrorMessage in result.ErrorMessages)
        {
            ModelState.AddModelError(resultErrorMessage.Key, resultErrorMessage.Value);
        }
    }
    
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Authenticate([FromBody] AuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            Console.WriteLine("haha");
            return BadRequest(ModelState);
        }

        var result = await authenticationService.LoginAsync(request.UserName, request.Password);

        if (!result.Success)
        {
            Console.WriteLine("hahe");
            AddErrors(result);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return BadRequest(ModelState);
        }

        return Ok(new AuthResponse(result.Email, result.UserName, result.Token));
    }

    [HttpPatch("Patch"), Authorize(Roles = "User, Admin")]
    public async Task<ActionResult<ChangeUserPasswordResponse>> ChangeUserPassword([FromBody] ChangeUserPasswordRequest request)
    {
        Console.WriteLine(request);
        try
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {request.Email} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }

            var result = await userManager.ChangePasswordAsync(existingUser, request.OldPassword, request.NewPassword);

            await repository.SaveChangesAsync();

            if (result.Succeeded)
            {
                await repository.SaveChangesAsync();
                return Ok($"Successful update on {request.Email}!");
            }
            else
            {
                logger.LogError($"Error changing password for user {request.Email}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing password for user {request.Email}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error delete sun data");
            return NotFound("Error delete sun data");
        }
    }
}