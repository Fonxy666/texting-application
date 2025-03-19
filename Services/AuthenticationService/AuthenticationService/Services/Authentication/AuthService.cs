using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthenticationService.Model;
using AuthenticationService.Model.Requests.Auth;
using AuthenticationService.Model.Responses.Auth;
using AuthenticationService.Services.Cookie;
using AuthenticationService.Services.PrivateKey;

namespace AuthenticationService.Services.Authentication;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ICookieService cookieService,
    IPrivateKeyService keyService
    ) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegistrationRequest request, string role, string imagePath)
    {
        var user = new ApplicationUser(imagePath)
        {
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PhoneNumberConfirmed = false,
            EmailConfirmed = true,
            TwoFactorEnabled = true,
            LockoutEnabled = false
        };
        
        user.SetPublicKey(request.PublicKey);

        var createResult = await userManager.CreateAsync(user, request.Password);

        var addToRoleAsync = await userManager.AddToRoleAsync(user, role);

        if (!createResult.Succeeded && !addToRoleAsync.Succeeded)
        {
            return new AuthResult(false, "", "");
        }
        
        var savedUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == request.Username);
        var privateKey = new Model.PrivateKey(request.EncryptedPrivateKey, request.Iv);
        var result = await keyService.SaveKey(privateKey, savedUser!.Id);

        return !result ? new AuthResult(false, "", "") : new AuthResult(true, "", "");
    }

    public async Task<AuthResult> LoginAsync(string username, bool rememberMe)
    {
        var managedUser = await userManager.FindByNameAsync(username);
        
        var roles = await userManager.GetRolesAsync(managedUser!);
        
        var accessToken = tokenService.CreateJwtToken(managedUser!, roles[0], rememberMe);
        
        if (rememberMe)
        {
            cookieService.SetRefreshToken(managedUser!);
            await userManager.UpdateAsync(managedUser!);
        }

        cookieService.SetRememberMeCookie(rememberMe);
        cookieService.SetPublicKey(rememberMe, managedUser!.PublicKey);
        cookieService.SetUserId(managedUser!.Id, rememberMe);
        cookieService.SetAnimateAndAnonymous(rememberMe);
        await cookieService.SetJwtToken(accessToken, rememberMe);
        
        return new AuthResult(true, managedUser.Id.ToString(), "");
    }

    public async Task<AuthResult> LoginWithExternal(string emailAddress)
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
        cookieService.SetPublicKey(true, managedUser.PublicKey);
        cookieService.SetUserId(managedUser.Id, true);
        cookieService.SetAnimateAndAnonymous(true);
        await cookieService.SetJwtToken(accessToken, true);
        
        return new AuthResult(true, managedUser.Id.ToString(), "");
    }

    public async Task<AuthResult> ExamineLoginCredentials(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return InvalidCredentials("Invalid username or password");
        }

        var lockoutResult = await ExamineLockoutEnabled(managedUser);

        if (!lockoutResult.Success)
        {
            return lockoutResult;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(managedUser);
            return InvalidCredentials(userManager.GetAccessFailedCountAsync(managedUser).Result.ToString());
        }
        
        await userManager.ResetAccessFailedCountAsync(managedUser);

        return new AuthResult(true, managedUser.Id.ToString(), managedUser.Email!);
    }

    private async Task<AuthResult> ExamineLockoutEnabled(ApplicationUser user)
    {
        var lockoutEndDate = await userManager.GetLockoutEndDateAsync(user);

        if (lockoutEndDate.HasValue && lockoutEndDate.Value > DateTimeOffset.Now)
        {
            return InvalidCredentials($"Account is locked. Try again after {lockoutEndDate.Value - DateTimeOffset.Now}");
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        
        var userLockout = userManager.GetAccessFailedCountAsync(user).Result >= 4;
        
        if (userLockout)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddDays(1));
            await userManager.ResetAccessFailedCountAsync(user);
            return InvalidCredentials("Account is locked. Try again after 1 day");
        }

        return new AuthResult(true, "", "");
    }

    public async Task<AuthResult> LogOut(string userId)
    {
        var user = userManager.Users.FirstOrDefault(user => user.Id.ToString() == userId);
        user!.SetRefreshToken(string.Empty);
        user.SetRefreshTokenCreated(null);
        user.SetRefreshTokenExpires(null);
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
}