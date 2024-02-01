using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Contracts;
using Server.Database;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.EmailSender;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    IAuthService authenticationService,
    UsersContext repository,
    ILogger<AuthController> logger,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : ControllerBase
{
    [HttpPost("GetEmailVerificationToken")]
    public async Task<ActionResult<GetEmailForVerificationResponse>> SendEmailVerificationCode([FromBody] GetEmailForVerificationRequest receiver)
    {
        var subject = "Verification code";
        var message = $"Verification code: {EmailSenderCodeGenerator.GenerateToken(receiver.Email)}";

        var result = await emailSender.SendEmailAsync(receiver.Email, subject, message);
        
        if (!result)
        {
            return BadRequest(ModelState);
        }

        return Ok(new GetEmailForVerificationResponse("Successfully send."));
    }

    [HttpPost("ExamineVerifyToken")]
    public async Task<ActionResult<string>> VerifyToken([FromBody] VerifyTokenRequest credentials)
    {
        var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(credentials.Email, credentials.VerifyCode);
        if (!result)
        {
            return BadRequest(ModelState);
        }
        
        EmailSenderCodeGenerator.RemoveVerificationCode(credentials.Email);
        return Ok(new VerifyTokenResponse(true));
    }
        
    [HttpPost("Register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var imagePath = SaveImageLocally(request.Username, request.Image);
        var result = await authenticationService.RegisterAsync(request.Email, request.Username, request.Password, "User", request.PhoneNumber, imagePath);

        if (!result.Success)
        {
            AddErrors(result);
            return BadRequest(ModelState);
        }

        return CreatedAtAction(nameof(Register), new RegistrationResponse(result.Email, result.UserName));
    }
    
    private string SaveImageLocally(string userNameFileName, string base64Image)
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var imageName = userNameFileName + ".png";
        var imagePath = Path.Combine(folderPath, imageName);

        try
        {
            base64Image = base64Image.Replace("data:image/png;base64,", "");
            var imageBytes = Convert.FromBase64String(base64Image);

            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                fileStream.Write(imageBytes, 0, imageBytes.Length);
            }

            return imagePath;
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding base64 image: {ex.Message}");
            throw;
        }
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
            return BadRequest(ModelState);
        }

        var result = await authenticationService.LoginAsync(request.UserName, request.Password);

        if (!result.Success)
        {
            AddErrors(result);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return BadRequest(ModelState);
        }

        return Ok(new AuthResponse(result.Email, result.UserName, result.Token));
    }

    [HttpPatch("ChangePassword")]
    public async Task<ActionResult<ChangeUserPasswordResponse>> ChangeUserPassword([FromBody] ChangeUserPasswordRequest request)
    {
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
                var response = new ChangeUserPasswordResponse(existingUser.Email!, existingUser.UserName!);
                return Ok(response);
            }
            else
            {
                logger.LogError($"Error changing password for user {request.Email}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing password for user {request.Email}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing password for user {request.Email}");
            return NotFound($"Error changing password for user {request.Email}");
        }
    }
    
    [HttpPatch("ChangeEmail")]
    public async Task<ActionResult<ChangeEmailRequest>> ChangeUserEmail([FromBody] ChangeEmailRequest request)
    {
        try
        {
;           var existingUser = await userManager.FindByEmailAsync(request.OldEmail);
            Console.WriteLine(existingUser);
            if (existingUser == null)
            {
                logger.LogInformation($"Data for email: {request.OldEmail} doesnt't exists in the database.");
                return BadRequest(ModelState);
            }
            
            var result = await userManager.ChangeEmailAsync(existingUser, request.NewEmail, request.Token);

            await repository.SaveChangesAsync();

            if (result.Succeeded)
            {
                await repository.SaveChangesAsync();
                var response = new ChangeEmailResponse(existingUser.Email!, existingUser.UserName!);
                return Ok(response);
            }
            else
            {
                logger.LogError($"Error changing e-mail for user {request.OldEmail}: {string.Join(", ", result.Errors)}");
                return BadRequest($"Error changing e-mail for user {request.OldEmail}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error changing e-mail for user {request.OldEmail}");
            return NotFound($"Error changing e-mail for user {request.OldEmail}");
        }
    }
}