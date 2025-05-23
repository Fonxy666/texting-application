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
using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(
    IAuthService authenticationService,
    IEmailSender emailSender,
    ILogger<AuthController> logger,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("SendEmailVerificationToken")]
    public async Task<ActionResult<ResponseBase>> SendEmailVerificationCode([FromBody]GetEmailForVerificationRequest receiver)
    {
        try
        {
            var emailResponse = await emailSender.SendEmailAsync(receiver.Email, EmailType.Registration);
            if (emailResponse is Failure)
            {
                return BadRequest(emailResponse);
            }

            return Ok(new SuccessWithMessage("Successfully sent."));
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
            var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(credentials.Email, credentials.VerifyCode, EmailType.Registration);
            if (!result)
            {
                return await Task.FromResult<ActionResult<ResponseBase>>(BadRequest(new FailureWithMessage("Invalid token.")));
            }
            
            EmailSenderCodeGenerator.RemoveVerificationCode(credentials.Email, EmailType.Registration);
            return await Task.FromResult<ActionResult<ResponseBase>>(Ok(new Success()));
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
            var result = await authenticationService.RegisterAsync(request);
            
            if (result is Failure || result is FailureWithMessage) 
            {
                return StatusCode(500, result);
            }
            
            if (result is FailureWithMessage)
            {
                return BadRequest(result);
            }

            return Ok(new Success());
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

            if (result is FailureWithMessage error)
            {
                return error.Message switch
                {
                    var msg when msg == $"{request.UserName} is not registered." => NotFound("This username is not registered."),
                    _ => BadRequest(error)
                };
            }
        
            var userEmail = (result as SuccessWithDto<UserEmailDto>)!.Data!.Email;

            var sentEmail = await emailSender.SendEmailAsync(userEmail, EmailType.Login);

            if (sentEmail is Failure)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(new SuccessWithDto<UserEmailDto>(new UserEmailDto(userEmail)));
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

            if (loginResult is FailureWithMessage)
            {
                return BadRequest(new FailureWithMessage("And error occured during login."));
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
                    var loginResponse = await authenticationService.LoginWithExternal(claim.Value);
                    if (loginResponse is FailureWithMessage error)
                    {
                        var message = Uri.EscapeDataString(error.Message);
                        return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=false&errorMessage={message}");
                    }
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
                    var loginResponse = await authenticationService.LoginWithExternal(claim.Value);
                    if (loginResponse is FailureWithMessage error)
                    {
                        var message = Uri.EscapeDataString(error.Message);
                        return Redirect($"{configuration["FrontendUrlAndPort"]}?loginSuccess=false&errorMessage={message}");
                    }
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
            var logoutResult = await authenticationService.LogOutAsync(userId);

            return Ok(logoutResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during logout.");
            return StatusCode(500);
        }
    }
}