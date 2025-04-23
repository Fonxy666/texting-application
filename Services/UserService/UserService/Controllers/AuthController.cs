using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using UserService.Models.Responses;
using UserService.Services.User;
using UserService.Services.EmailSender;
using UserService.Services.Authentication;
using UserService.Models.Requests;
using UserService.Filters;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(
    IAuthService authenticationService,
    IApplicationUserService userServices,
    IEmailSender emailSender,
    ILogger<AuthController> logger,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("SendEmailVerificationToken")]
    public async Task<ActionResult<ResponseBase>> SendEmailVerificationCode([FromBody]GetEmailForVerificationRequest receiver)
    {
        try
        {
            var emailResponse = await emailSender.SendEmailAsync(receiver.Email, "registration");
            if (emailResponse is FailedResponse)
            {
                return BadRequest(emailResponse);
            }

            return Ok(new AuthResponseSuccessWithMessage("Successfully sent."));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending e-mail for : {receiver}.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("ExamineVerifyToken")]
    public async Task<ActionResult<ResponseBase>> VerifyToken([FromBody]VerifyTokenRequest credentials)
    {
        try
        {
            var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(credentials.Email, credentials.VerifyCode, "registration");
            if (!result)
            {
                return await Task.FromResult<ActionResult<ResponseBase>>(BadRequest(new FailedResponseWithMessage("Invalid token.")));
            }
            
            EmailSenderCodeGenerator.RemoveVerificationCode(credentials.Email, "registration");
            return await Task.FromResult<ActionResult<ResponseBase>>(Ok(new AuthResponseSuccess()));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Wrong credit for e-mail : {credentials.Email}.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("Register")]
    public async Task<ActionResult<ResponseBase>> Register([FromBody]RegistrationRequest request)
    {
        try
        {
            var imagePath = userServices.SaveImageLocally(request.Username, request.Image);

            if (imagePath is FailedResponseWithMessage error)
            {
                return BadRequest(error.Message);
            }

            var result = await authenticationService.RegisterAsync(request, (imagePath as UserResponseSuccessWithMessage)!.Message);

            if (result is FailedResponse)
            {
                return StatusCode(500, result);
            }

            return Ok(new AuthResponseSuccess());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during registration.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("SendLoginToken")]
    public async Task<ActionResult<ResponseBase>> SendLoginToken([FromBody]AuthRequest request)
    {
        try
        {
            var result = await authenticationService.ExamineLoginCredentialsAsync(request.UserName, request.Password);
        
            if (result is FailedResponseWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"{request.UserName} is not registered." => NotFound(error.Message),
                    "The provided login code is not correct." => BadRequest(error.Message),
                    _ => BadRequest(error)
                };
            }
        
            var successResult = result as AuthResponseWithEmailSuccess;

            await emailSender.SendEmailAsync(successResult!.Email, "login");

            return Ok(new AuthResponseSuccessWithId(successResult.Id));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during sending login token for user: {request.UserName}");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("Login")]
    public async Task<ActionResult<ResponseBase>> Login([FromBody]LoginAuth request)
    {
        try
        {
            var loginResult = await authenticationService.LoginAsync(request);

            if (loginResult is FailedResponseWithMessage error)
            {
                return BadRequest(error.Message);
            }

            return Ok(loginResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during login for user: {request.UserName}");
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("LoginWithFacebook")]
    [ExcludeFromCodeCoverage]
    public async Task FacebookLogin()
    {
        try
        {
            await HttpContext.ChallengeAsync(FacebookDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(FacebookResponse))
                });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during facebook login.");
        }
    }
    
    [HttpGet("FacebookResponse")]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> FacebookResponse()
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal!.Identities.FirstOrDefault()!.Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });
            
            foreach (var claim in claims)
            {
                var splittedClaim = claim.Type.Split("/");
                if (splittedClaim[^1] == "emailaddress")
                {
                    await authenticationService.LoginWithExternal(claim.Value);
                    break;
                }
            }
            
            return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=true");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during facebook login.");
            return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=false");
        }
    }
    
    [HttpGet("LoginWithGoogle")]
    [ExcludeFromCodeCoverage]
    public async Task GoogleLogin()
    {
        try
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(GoogleResponse))
                });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during google login.");
        }
    }

    [HttpGet("GoogleResponse")]
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> GoogleResponse()
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal!.Identities.FirstOrDefault()!.Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });

            foreach (var claim in claims)
            {
                var splittedClaim = claim.Type.Split("/");
                if (splittedClaim[^1] == "emailaddress")
                {
                    await authenticationService.LoginWithExternal(claim.Value);
                    break;
                }
            }
            
            return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=true");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during google login.");
            return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=false");
        }
    }
    
    [HttpGet("Logout")]
    [RequireUserIdCookie]
    public async Task<ActionResult<ResponseBase>> Logout()
    {
        try
        {
            var userId = (Guid)HttpContext.Items["UserId"]!;
            var logoutResult = await authenticationService.LogOutAsync(userId!);

            return Ok(logoutResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during logout.");
            return StatusCode(500);
        }
    }
}