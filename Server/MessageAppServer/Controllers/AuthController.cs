﻿using Microsoft.AspNetCore.Mvc;
using Server.Requests;
using Server.Responses;
using Server.Services.Authentication;
using Server.Services.EmailSender;
using Microsoft.AspNetCore.Authorization;
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
        var message = $"Verification code: {EmailSenderCodeGenerator.GenerateToken(receiver.Email)}";

        var result = await emailSender.SendEmailAsync(receiver.Email, subject, message);
        
        if (!result)
        {
            return BadRequest(ModelState);
        }

        return Ok(new GetEmailForVerificationResponse("Successfully send."));
    }

    [HttpPost("ExamineVerifyToken")]
    public async Task<ActionResult<string>> VerifyToken([FromBody]VerifyTokenRequest credentials)
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
            return NotFound(ModelState);
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
    
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Authenticate([FromBody]AuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.LoginAsync(request.UserName, request.Password, request.RememberMe);

        if (!result.Success)
        {
            AddErrors(result);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return NotFound(ModelState);
        }

        return Ok(new AuthResponse(true, result.Id));
    }
    
    [HttpPost("Logout")]
    public async Task<ActionResult<AuthResponse>> LogOut()
    {
        var result = await authenticationService.LogOut();

        if (!result.Success)
        {
            AddErrors(result);
            return NotFound(ModelState);
        }

        return Ok(new AuthResponse(true, ""));
    }
}