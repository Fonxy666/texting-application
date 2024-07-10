using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Model;
using Server.Model.Requests.Auth;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.Authentication;
using Server.Services.EmailSender;
using Server.Services.PrivateKey;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(
    IAuthService authenticationService,
    IUserServices userServices,
    IEmailSender emailSender,
    ILogger<AuthController> logger,
    UserManager<ApplicationUser> userManager,
    IPrivateKeyService privateKeyService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("SendEmailVerificationToken")]
    public async Task<ActionResult<string>> SendEmailVerificationCode([FromBody]GetEmailForVerificationRequest receiver)
    {
        try
        {
            const string subject = "Verification code";
            var message = $"Verification code: {EmailSenderCodeGenerator.GenerateLongToken(receiver.Email, "registration")}";

            await emailSender.SendEmailAsync(receiver.Email, subject, message);

            return Ok("Successfully sent.");
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error sending e-mail for : {receiver}.");
            return StatusCode(500);
        }
    }

    [HttpPost("ExamineVerifyToken")]
    public async Task<ActionResult<string>> VerifyToken([FromBody]VerifyTokenRequest credentials)
    {
        try
        {
            var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(credentials.Email, credentials.VerifyCode, "registration");
            if (!result)
            {
                return await Task.FromResult<ActionResult<string>>(BadRequest("Invalid e-mail or token."));
            }
            
            EmailSenderCodeGenerator.RemoveVerificationCode(credentials.Email, "registration");
            return await Task.FromResult<ActionResult<string>>(Ok(true));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Wrong credit for e-mail : {credentials.Email}.");
            return StatusCode(500);
        }
    }
        
    [HttpPost("Register")]
    public async Task<ActionResult<EmailUsernameResponse>> Register(RegistrationRequest request)
    {
        try
        {
            var imagePath = userServices.SaveImageLocally(request.Username, request.Image);
            var result = await authenticationService.RegisterAsync(request, "User", imagePath);

            return Ok(new AuthResponse(true, result.Id));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during registration.");
            return BadRequest("Error during registration.");
        }
    }
    
    [HttpPost("SendLoginToken")]
    public async Task<ActionResult<AuthResponse>> SendLoginToken([FromBody]AuthRequest request)
    {
        try
        {
            var result = await authenticationService.ExamineLoginCredentials(request.UserName, request.Password);
        
            if (result is FailedAuthResult failedResult)
            {
                return BadRequest(failedResult.AdditionalInfo);
            }
        
            const string subject = "Verification code";
            var message = $"{subject}: {EmailSenderCodeGenerator.GenerateShortToken(result.Email, "login")}";

            await emailSender.SendEmailAsync(result.Email, subject, message);

            return Ok(new AuthResponse(true, result.Id));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during sending login token for user: {request.UserName}");
            return BadRequest($"Error during sending login token for user: {request.UserName}");
        }
    }
    
    [HttpPost("Login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody]LoginAuth request)
    {
        try
        {
            var existingUser = await userManager.FindByNameAsync(request.UserName);
            if (existingUser == null)
            {
                return BadRequest("Invalid credentials");
            }
            
            var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(existingUser.Email!, request.Token, "login");
        
            if (!result)
            {
                return BadRequest(new AuthResponse(false, "Bad request"));
            }

            EmailSenderCodeGenerator.RemoveVerificationCode(existingUser.Email!, "login");

            var encryptedPrivateKey = await privateKeyService.GetEncryptedKeyByUserIdAsync(existingUser.Id);

            var loginResult = await authenticationService.LoginAsync(request.UserName, request.RememberMe);

            if (!loginResult.Success)
            {
                return StatusCode(500);
            }

            return Ok(new LoginResponse(true, existingUser.PublicKey, encryptedPrivateKey));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during login for user: {request.UserName}");
            return StatusCode(500);
        }
    }
    
    [ExcludeFromCodeCoverage]
    [HttpGet("LoginWithFacebook")]
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
    
    [ExcludeFromCodeCoverage]
    [HttpGet("FacebookResponse")]
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
    
    [ExcludeFromCodeCoverage]
    [HttpGet("LoginWithGoogle")]
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

    [ExcludeFromCodeCoverage]
    [HttpGet("GoogleResponse")]
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
    public async Task<ActionResult<AuthResponse>> LogOut()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            await authenticationService.LogOut(userId!);

            return Ok(new AuthResponse(true, userId!));
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error during logout.");
            return StatusCode(500);
        }
    }
}