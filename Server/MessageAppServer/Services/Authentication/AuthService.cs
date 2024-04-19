﻿using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Model.Responses.Auth;
using Server.Model.Responses.User;
using Server.Services.Cookie;

namespace Server.Services.Authentication;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ICookieService cookieService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image)
    {
        var user = new ApplicationUser(image)
        {
            UserName = username,
            Email = email,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = false,
            EmailConfirmed = true,
            TwoFactorEnabled = true,
            LockoutEnabled = false
        };
        
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return FailedRegistration(result);
        }

        await userManager.AddToRoleAsync(user, role);
        return new AuthResult(true, "", "");
    }

    private static AuthResult FailedRegistration(IdentityResult result)
    {
        var authenticationResult = new AuthResult(false, "", "");

        foreach (var identityError in result.Errors)
        {
            authenticationResult.ErrorMessages.Add(identityError.Code, identityError.Description);
        }

        return authenticationResult;
    }

    public async Task<AuthResult> LoginAsync(string username, bool rememberMe)
    {
        var managedUser = await userManager.FindByNameAsync(username);
        
        if (managedUser == null)
        {
            return new AuthResult(false, "", "");
        }
        
        var roles = await userManager.GetRolesAsync(managedUser);
        var accessToken = tokenService.CreateJwtToken(managedUser, roles[0], rememberMe);
        
        if (rememberMe)
        {
            cookieService.SetRefreshToken(managedUser);
            await userManager.UpdateAsync(managedUser);
        }

        cookieService.SetRememberMeCookie(rememberMe);
        cookieService.SetUserId(managedUser.Id, rememberMe);
        cookieService.SetAnimateAndAnonymous(rememberMe);
        await cookieService.SetJwtToken(accessToken, rememberMe);
        
        return new AuthResult(true, managedUser.Id, "");
    }

    public async Task<AuthResult> LoginWithGoogle(string emailAddress)
    {
        var managedUser = await userManager.FindByEmailAsync(emailAddress);
        
        if (managedUser == null)
        {
            return new AuthResult(false, "", "");
        }
        
        var roles = await userManager.GetRolesAsync(managedUser);
        var accessToken = tokenService.CreateJwtToken(managedUser, roles[0], true);
        
        cookieService.SetRefreshToken(managedUser);
        await userManager.UpdateAsync(managedUser);

        cookieService.SetRememberMeCookie(true);
        cookieService.SetUserId(managedUser.Id, true);
        cookieService.SetAnimateAndAnonymous(true);
        await cookieService.SetJwtToken(accessToken, true);
        
        return new AuthResult(true, managedUser.Id, "");
    }

    public Task<string?> GetEmailFromUserName(string username)
    {
        return Task.FromResult(userManager.Users.FirstOrDefault(user => user.UserName == username)?.Email);
    }

    public async Task<AuthResult> ExamineLoginCredentials(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return InvalidCredentials("Invalid username or password");
        }
        
        var lockoutEndDate = await userManager.GetLockoutEndDateAsync(managedUser);

        if (lockoutEndDate.HasValue && lockoutEndDate.Value > DateTimeOffset.Now)
        {
            return InvalidCredentials($"Account is locked. Try again after {lockoutEndDate.Value - DateTimeOffset.Now}");
        }

        await userManager.SetLockoutEndDateAsync(managedUser, null);
        
        var userLockout = userManager.GetAccessFailedCountAsync(managedUser).Result >= 4;
        
        if (userLockout)
        {
            await userManager.SetLockoutEndDateAsync(managedUser, DateTimeOffset.Now.AddDays(1));
            await userManager.ResetAccessFailedCountAsync(managedUser);
            return InvalidCredentials($"Account is locked. Try again after 1 day");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(managedUser);
            return InvalidCredentials(userManager.GetAccessFailedCountAsync(managedUser).Result.ToString());
        }
        
        await userManager.ResetAccessFailedCountAsync(managedUser);

        return new AuthResult(true, managedUser.Id, managedUser.Email!);
    }

    public async Task<AuthResult> LogOut(string userId)
    {
        var user = userManager.Users.FirstOrDefault(user => user.Id == userId);
        user!.RefreshToken = string.Empty;
        user.RefreshTokenExpires = null;
        user.RefreshTokenCreated = null;
        await userManager.UpdateAsync(user);
        cookieService.DeleteCookies();
        return new AuthResult(true, "-", "-");
    }

    private FailedAuthResult InvalidCredentials(string message)
    {
        var result = new FailedAuthResult(false, "-", "-", message);
        result.ErrorMessages.Add("Bad credentials", "Invalid email");
        return result;
    }
    
    public async Task<DeleteUserResponse> DeleteAsync(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return new DeleteUserResponse($"{username}", "Doesn't exist in the database", false);
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            return new DeleteUserResponse($"{username}", "For this user, the given credentials doesn't match.", false);
        }

        await userManager.DeleteAsync(managedUser);

        return new DeleteUserResponse($"{username}", "Delete successful.", true);
    }
}