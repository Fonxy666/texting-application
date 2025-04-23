using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.PrivateKeyFolder;

namespace UserService.Services.Authentication;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ICookieService cookieService,
    IPrivateKeyService keyService,
    ILogger<AuthService> logger,
    IPrivateKeyService privateKeyService,
    MainDatabaseContext context
    ) : IAuthService
{
    public async Task<ResponseBase> RegisterAsync(RegistrationRequest request, string imagePath)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
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

            if (!createResult.Succeeded)
            {
                logger.LogError("Error during database save.");
                return new FailedResponse();
            }

            var addToRoleAsync = await userManager.AddToRoleAsync(user, "User");

            if (!addToRoleAsync.Succeeded)
            {
                logger.LogError("Error during adding user to role.");
                return new FailedResponse();
            }

            var savedUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == request.Username);
            var privateKey = new PrivateKey(request.EncryptedPrivateKey, request.Iv);
            var keyResult = await keyService.SaveKeyAsync(privateKey, savedUser!.Id);

            if (keyResult is FailedResponse)
            {
                logger.LogError("Error during key save.");
                return new FailedResponse();
            }

            await transaction.CommitAsync();

            return keyResult.IsSuccess ? new AuthResponseSuccess() : new FailedResponse();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registration failed during transaction.");
            return new FailedResponse();
        }
    }

    public async Task<ResponseBase> LoginAsync(LoginAuth request)
    {
        var (userName, rememberMe, token) = request;

        var existingUser = await userManager.FindByNameAsync(userName);

        var codeExamineResult = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(existingUser!.Email!, token, "login");

        if (!codeExamineResult)
        {
            logger.LogError("The provided login code is not correct.");
            return new FailedResponseWithMessage("The provided login code is not correct.");
        }

        var keyRetrievalResult = await privateKeyService.GetEncryptedKeyByUserIdAsync(existingUser.Id.ToString());
        if (keyRetrievalResult is FailedResponse)
        {
            logger.LogError("Cannot find User private key.");
            return new FailedResponseWithMessage("Cannot find User private key.");
        }
        var encryptedKey = (keyRetrievalResult as PrivateKeyResponseSuccessWithIv)!.EncryptedKey;

        var roles = await userManager.GetRolesAsync(existingUser);
        
        var accessToken = tokenService.CreateJwtToken(existingUser!, roles[0], request.RememberMe);

        if (request.RememberMe)
        {
            cookieService.SetRefreshToken(existingUser);
            await userManager.UpdateAsync(existingUser);
        }

        cookieService.SetRememberMeCookie(rememberMe);
        cookieService.SetPublicKey(rememberMe, existingUser.PublicKey);
        cookieService.SetUserId(existingUser.Id, rememberMe);
        cookieService.SetAnimateAndAnonymous(rememberMe);
        await cookieService.SetJwtToken(accessToken, rememberMe);
        
        return new LoginResponseSuccess(existingUser.PublicKey, encryptedKey);
    }

    public async Task<ResponseBase> LoginWithExternal(string emailAddress)
    {
        var managedUser = await userManager.FindByEmailAsync(emailAddress);
        
        if (managedUser == null)
        {
            return new FailedResponse();
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
        
        return new AuthResponseSuccessWithId(managedUser.Id.ToString());
    }

    public async Task<ResponseBase> ExamineLoginCredentialsAsync(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            logger.LogError("This username is not registered.");
            return new FailedResponse();
        }

        var lockoutResult = await ExamineLockoutEnabled(managedUser);

        if (lockoutResult is FailedResponseWithMessage error)
        {
            logger.LogError(error.Message);
            return lockoutResult;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(managedUser);
            logger.LogError("Invalid password.");
            return new FailedResponse();
        }
        
        await userManager.ResetAccessFailedCountAsync(managedUser);

        return new AuthResponseWithEmailSuccess(managedUser.Id.ToString(), managedUser.Email!);
    }

    private async Task<ResponseBase> ExamineLockoutEnabled(ApplicationUser user)
    {
        var lockoutEndDate = await userManager.GetLockoutEndDateAsync(user);

        if (lockoutEndDate.HasValue && lockoutEndDate.Value > DateTimeOffset.Now)
        {
            return new FailedResponseWithMessage($"Account is locked. Try again after {lockoutEndDate.Value - DateTimeOffset.Now}");
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        
        var userLockout = userManager.GetAccessFailedCountAsync(user).Result >= 4;
        
        if (userLockout)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddDays(1));
            await userManager.ResetAccessFailedCountAsync(user);
            return new FailedResponseWithMessage("Account is locked. Try again after 1 day");
        }

        return new AuthResponseSuccess();
    }

    public async Task<ResponseBase> LogOutAsync(string userId)
    {
        var user = userManager.Users.FirstOrDefault(user => user.Id.ToString() == userId);
        user!.SetRefreshToken(string.Empty);
        user.SetRefreshTokenCreated(null);
        user.SetRefreshTokenExpires(null);
        await userManager.UpdateAsync(user);
        cookieService.DeleteCookies();
        return new AuthResponseSuccess();
    }

    public Task<ResponseBase> LogOutAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}