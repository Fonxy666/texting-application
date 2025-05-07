using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Models.Requests;
using UserService.Models.Responses;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.PrivateKeyFolder;
using Textinger.Shared.Responses;

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
        
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var userCreationResult = await CreateUser(request, imagePath);
            if (userCreationResult is FailureWithMessage)
            {
                return userCreationResult;
            }

            var saveKeyResult = await SavePrivateKey(request);
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
        var existingUserByEmail = await context.Users.AnyAsync(u => u.Email == request.Email);
        if (existingUserByEmail)
        {
            return new FailureWithMessage("Email is already taken");
        }
        
        var existingUserByUsername = await context.Users.AnyAsync(u => u.UserName == request.Username);
        
        if (existingUserByUsername)
        {
            return new FailureWithMessage("Username is already taken");
        }
        
        var existingUserByPhoneNumber = await context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (existingUserByPhoneNumber)
        {
            return new FailureWithMessage("Phone number is already taken");
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

        return new Success();
    }

    private async Task<ResponseBase> SavePrivateKey(RegistrationRequest request)
    {
        var savedUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == request.Username);
        var privateKey = new PrivateKey(request.EncryptedPrivateKey, request.Iv);
        var keyResult = await keyService.SaveKeyAsync(privateKey, savedUser!.Id);

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

        var codeExamineResult = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(existingUser!.Email!, token, "login");

        if (!codeExamineResult)
        {
            logger.LogError("The provided login code is not correct.");
            return new FailureWithMessage("The provided login code is not correct.");
        }

        var keyRetrievalResult = await privateKeyService.GetEncryptedKeyByUserIdAsync(existingUser.Id.ToString());
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
        var managedUser = await userManager.FindByEmailAsync(emailAddress);
        
        if (managedUser == null)
        {
            return new Failure();
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
        
        return new SuccessWithDto<UserIdDto>(new UserIdDto(managedUser.Id.ToString()));
    }

    public async Task<ResponseBase> ExamineLoginCredentialsAsync(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            logger.LogError("This username is not registered.");
            return new FailureWithMessage($"{username} is not registered.");
        }

        var lockoutResult = await ExamineLockoutEnabled(managedUser);

        if (lockoutResult is FailureWithMessage error)
        {
            logger.LogError(error.Message);
            return lockoutResult;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(managedUser);
            var accessFailedCount = await userManager.GetAccessFailedCountAsync(managedUser);
            logger.LogError("Invalid password.");
            return new FailureWithMessage($"Invalid credentials, u have {5 - accessFailedCount} more tries.");
        }


        await userManager.ResetAccessFailedCountAsync(managedUser);

        return new SuccessWithDto<UserNameEmailDto>(new UserNameEmailDto(managedUser.Id.ToString(), managedUser.Email!));
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

    public async Task<ResponseBase> LogOutAsync(string userId)
    {
        var user = userManager.Users.FirstOrDefault(user => user.Id.ToString() == userId);
        user!.SetRefreshToken(string.Empty);
        user.SetRefreshTokenCreated(null);
        user.SetRefreshTokenExpires(null);
        await userManager.UpdateAsync(user);
        cookieService.DeleteCookies();
        return new Success();
    }
}