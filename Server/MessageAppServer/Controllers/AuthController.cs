﻿using Microsoft.AspNetCore.Mvc;
using Server.Requests.Auth;
using Server.Responses.Auth;
using Server.Responses.User;
using Server.Services.Authentication;
using Server.Services.EmailSender;
using Server.Services.User;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    IAuthService authenticationService,
    IUserServices userServices,
    IEmailSender emailSender) : ControllerBase
{
    [HttpPost("GetEmailVerificationToken")]
    public async Task<ActionResult<GetEmailForVerificationResponse>> SendEmailVerificationCode([FromBody]GetEmailForVerificationRequest receiver)
    {
        const string subject = "Verification code";
        var message = $"Verification code: {EmailSenderCodeGenerator.GenerateTokenForRegistration(receiver.Email)}";

        var result = await emailSender.SendEmailAsync(receiver.Email, subject, message);
        
        if (!result)
        {
            return BadRequest(ModelState);
        }

        return Ok(new GetEmailForVerificationResponse("Successfully send."));
    }

    [HttpGet("CheckCookies")]
    public ActionResult CheckCookies()
    {
        var userIdExists = Request.Cookies.ContainsKey("UserId");
        var refreshTokenExists = Request.Cookies.ContainsKey("RefreshToken");

        return Ok(userIdExists && refreshTokenExists);
    }

    [HttpPost("ExamineVerifyToken")]
    public async Task<ActionResult<string>> VerifyToken([FromBody]VerifyTokenRequest credentials)
    {
        var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(credentials.Email, credentials.VerifyCode, "registration");
        if (!result)
        {
            return BadRequest(ModelState);
        }
        
        EmailSenderCodeGenerator.RemoveVerificationCode(credentials.Email, "registration");
        return Ok(new VerifyTokenResponse(true));
    }
        
    [HttpPost("Register")]
    public async Task<ActionResult<EmailUsernameResponse>> Register(RegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var imagePath = userServices.SaveImageLocally(request.Username, request.Image);
        var result = await authenticationService.RegisterAsync(request.Email, request.Username, request.Password, "User", request.PhoneNumber, imagePath);

        if (!result.Success)
        {
            AddErrors(result);
            return BadRequest(ModelState);
        }

        return Ok(new AuthResponse(true, result.Id));
    }

    private void AddErrors(AuthResult result)
    {
        foreach (var resultErrorMessage in result.ErrorMessages)
        {
            ModelState.AddModelError(resultErrorMessage.Key, resultErrorMessage.Value);
        }
    }
    
    [HttpPost("SendLoginToken")]
    public async Task<ActionResult<AuthResponse>> SendLoginToken([FromBody]AuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.ExamineLoginCredentials(request.UserName, request.Password, request.RememberMe);
        
        if (result is FailedAuthResult failedResult)
        {
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            return NotFound(failedResult.AdditionalInfo);
        }
        
        const string subject = "Verification code";
        var message = $"Verification code: {EmailSenderCodeGenerator.GenerateTokenForLogin(result.Email)}";

        var emailResult = await emailSender.SendEmailAsync(result.Email, subject, message);

        return Ok(new AuthResponse(emailResult, result.Id));
    }
    
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody]LoginAuth request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var email = await authenticationService.GetEmailFromUserName(request.UserName);
        var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email!, request.Token, "login");
        
        if (!result)
        {
            return BadRequest(new AuthResponse(false, "loginResult.Id"));
        }

        EmailSenderCodeGenerator.RemoveVerificationCode(email!, "login");

        var loginResult = await authenticationService.LoginAsync(request.UserName, request.RememberMe);

        if (!loginResult.Success)
        {
            AddErrors(loginResult);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return NotFound(ModelState);
        }

        return Ok(new AuthResponse(true, loginResult.Id));
    }
    
    [HttpPost("Logout")]
    public async Task<ActionResult<AuthResponse>> LogOut([FromQuery]string userId)
    {
        var result = await authenticationService.LogOut(userId);
        if (!result.Success)
        {
            AddErrors(result);
            return NotFound(ModelState);
        }

        return Ok(new AuthResponse(true, userId));
    }
}