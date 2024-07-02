﻿using Server.Model.Responses.Auth;

namespace Server.Services.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image);
    Task<AuthResult> LoginAsync(string username, bool rememberMe);
    Task<AuthResult> LoginWithExternal(string emailAddress);
    Task<string?> GetEmailFromUserName(string username);
    Task<AuthResult> ExamineLoginCredentials(string username, string password);
    Task<AuthResult> LogOut(string userId);
}