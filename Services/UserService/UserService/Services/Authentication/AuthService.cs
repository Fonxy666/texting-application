using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.PrivateKeyFolder;
using Textinger.Shared.Responses;
using UserService.Helpers;

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
        var validateUserInputResult = await ValidateUserInput(request);
        if (validateUserInputResult is FailureWithMessage)
        {
            return validateUserInputResult;
        }
        
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var userCreationResult = await CreateUser(request, imagePath);
            if (userCreationResult is FailureWithMessage)
            {
                return userCreationResult;
            }

            var saveKeyResult = await SavePrivateKey(request, (userCreationResult as SuccessWithDto<UserIdDto>)!.Data!.Id);
            if (saveKeyResult is FailureWithMessage)
            {
                return saveKeyResult;
            }

            await transaction.CommitAsync();

            return saveKeyResult.IsSuccess ? new Success() : new Failure();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registration failed during transaction.");
            return new Failure();
        }
    }

    private async Task<ResponseBase> ValidateUserInput(RegistrationRequest request)
    {
        var conflictingUser = await context.Users
            .Where(u => u.Email == request.Email 
                        || u.UserName == request.Username 
                        || u.PhoneNumber == request.PhoneNumber)
            .FirstOrDefaultAsync();

        if (conflictingUser != null)
        {
            if (conflictingUser.Email == request.Email)
            {
                return new FailureWithMessage("Email is already taken");
            }

            if (conflictingUser.UserName == request.Username)
            {
                return new FailureWithMessage("Username is already taken");
            }

            if (conflictingUser.PhoneNumber == request.PhoneNumber)
            {
                return new FailureWithMessage("Phone number is already taken");
            }
        }

        return new Success();
    }

    private async Task<ResponseBase> CreateUser(RegistrationRequest request, string imagePath)
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
            return new Failure();
        }

        var addToRoleAsync = await userManager.AddToRoleAsync(user, "User");

        if (!addToRoleAsync.Succeeded)
        {
            logger.LogError("Error during adding user to role.");
            return new Failure();
        }

        return new SuccessWithDto<UserIdDto>(new UserIdDto(user.Id));
    }

    private async Task<ResponseBase> SavePrivateKey(RegistrationRequest request, Guid userId)
    {
        var privateKey = new PrivateKey(request.EncryptedPrivateKey, request.Iv);
        var keyResult = await keyService.SaveKeyAsync(privateKey, userId);

        if (keyResult is Failure)
        {
            logger.LogError("Error during key save.");
            return new Failure();
        }

        return new Success();
    }

    public async Task<ResponseBase> LoginAsync(LoginAuth request)
    {
        var (userName, rememberMe, token) = request;

        var existingUser = await userManager.FindByNameAsync(userName);

        var codeExamineResult = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(existingUser!.Email!, token, EmailType.Login);

        if (!codeExamineResult)
        {
            logger.LogError("The provided login code is not correct.");
            return new FailureWithMessage("The provided login code is not correct.");
        }

        var keyRetrievalResult = await privateKeyService.GetEncryptedKeyByUserIdAsync(existingUser.Id);
        if (keyRetrievalResult is Failure)
        {
            logger.LogError("Cannot find User private key.");
            return new FailureWithMessage("Cannot find User private key.");
        }
        var encryptedKey = (keyRetrievalResult as SuccessWithDto<KeyAndIvDto>)!.Data!.EncryptedPrivateKey;

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
        
        return new SuccessWithDto<KeysDto>(new KeysDto(existingUser.PublicKey, encryptedKey));
    }

    public async Task<ResponseBase> LoginWithExternal(string emailAddress)
    {
        var existingUser = await userManager.FindByEmailAsync(emailAddress);
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found");
        }
        
        var roles = await userManager.GetRolesAsync(existingUser);
        var accessToken = tokenService.CreateJwtToken(existingUser, roles[0], true);
        
        cookieService.SetRefreshToken(existingUser);
        await userManager.UpdateAsync(existingUser);

        cookieService.SetRememberMeCookie(true);
        cookieService.SetPublicKey(true, existingUser.PublicKey);
        cookieService.SetUserId(existingUser.Id, true);
        cookieService.SetAnimateAndAnonymous(true);
        await cookieService.SetJwtToken(accessToken, true);
        
        return new SuccessWithDto<UserIdDto>(new UserIdDto(existingUser.Id));
    }

    public async Task<ResponseBase> ExamineLoginCredentialsAsync(string userName, string password)
    {
        var existingUser = await userManager.FindByNameAsync(userName);
        if (existingUser is null)
        {
            return new FailureWithMessage($"{userName} is not registered.");
        }
        
        var lockoutResult = await ExamineLockoutEnabled(existingUser);

        if (lockoutResult is FailureWithMessage error)
        {
            logger.LogError(error.Message);
            return lockoutResult;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(existingUser, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(existingUser);
            var accessFailedCount = await userManager.GetAccessFailedCountAsync(existingUser);
            logger.LogError("Invalid password.");
            return new FailureWithMessage($"Invalid credentials, u have {5 - accessFailedCount} more trie(s).");
        }


        await userManager.ResetAccessFailedCountAsync(existingUser);

        return new SuccessWithDto<UserEmailDto>(new UserEmailDto(existingUser.Email!));
    }

    private async Task<ResponseBase> ExamineLockoutEnabled(ApplicationUser user)
    {
        var lockoutEndDate = await userManager.GetLockoutEndDateAsync(user);

        if (lockoutEndDate.HasValue && lockoutEndDate.Value > DateTimeOffset.UtcNow)
        {
            return new FailureWithMessage($"Account is locked. Try again after {lockoutEndDate.Value - DateTimeOffset.Now}");
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        
        var userLockout = userManager.GetAccessFailedCountAsync(user).Result >= 4;

        if (userLockout)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1));
            await userManager.ResetAccessFailedCountAsync(user);
            return new FailureWithMessage("Account is locked. Try again after 1 day");
        }

        return new Success();
    }

    public async Task<ResponseBase> LogOutAsync(Guid userId)
    {
        var existingUser = await userManager.FindByIdAsync(userId.ToString());
        if (existingUser is null)
        {
            return new FailureWithMessage("User not found");
        }
        
        existingUser.SetRefreshToken(string.Empty);
        existingUser.SetRefreshTokenCreated(null);
        existingUser.SetRefreshTokenExpires(null);
        await userManager.UpdateAsync(existingUser);
        cookieService.DeleteCookies();
        return new Success();
    }
}