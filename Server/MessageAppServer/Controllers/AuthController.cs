﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Server.Requests;
using Server.Responses;
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
    
    [HttpPost("SendLoginToken")]
    public async Task<ActionResult<AuthResponse>> SendLoginToken([FromBody]AuthRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.ExamineLoginCredentials(request.UserName, request.Password, request.RememberMe);

        if (!result.Success)
        {
            AddErrors(result);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return NotFound(ModelState);
        }
        
        const string subject = "Verification code";
        var message = $"Verification code: {EmailSenderCodeGenerator.GenerateTokenForLogin(result.Email)}";

        var emailResult = await emailSender.SendEmailAsync(result.Email, subject, message);

        return Ok(new AuthResponse(emailResult, result.Id));
    }

    public record LoginAuth([Required]string UserName, [Required]string Password, [Required]bool RememberMe, [Required]string Token);
    
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Authenticate([FromBody]LoginAuth request)
    {
        Console.WriteLine(request);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var email = await authenticationService.GetEmailFromUserName(request.UserName);
        var result =  EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email!, request.Token, "login");
        
        if (!result)
        {
            return BadRequest(ModelState);
        }

        EmailSenderCodeGenerator.RemoveVerificationCode(email!, "login");

        var loginResult = await authenticationService.LoginAsync(request.UserName, request.Password, request.RememberMe);

        if (!loginResult.Success)
        {
            AddErrors(loginResult);
            ModelState.AddModelError("InvalidCredentials", "Invalid username or password");
            
            return NotFound(ModelState);
        }

        return Ok(new AuthResponse(true, loginResult.Id));
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