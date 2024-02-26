using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Responses;
using Server.Responses.User;
using Server.Services.User;

namespace Server.Services.Authentication;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IUserServices userServices,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image)
    {
        var user = new ApplicationUser(image)
        {
            UserName = username,
            Email = email,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = false,
            EmailConfirmed = true
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

    public async Task<AuthResult> LoginAsync(string username, string password, bool rememberMe)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return InvalidCredentials();
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            return InvalidCredentials();
        }

        var roles = await userManager.GetRolesAsync(managedUser);
        var accessToken = tokenService.CreateToken(managedUser, roles[0]);
        
        tokenService.SetCookies(accessToken, managedUser.Id, rememberMe);

        return new AuthResult(true, managedUser.Id, "");
    }

    public Task<string?> GetEmailFromUserName(string username)
    {
        return Task.FromResult(userManager.Users.FirstOrDefault(user => user.UserName == username)?.Email);
    }

    public async Task<AuthResult> ExamineLoginCredentials(string username, string password, bool rememberMe)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return InvalidCredentials();
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            return InvalidCredentials();
        }

        return new AuthResult(true, managedUser.Id, managedUser.Email);
    }

    public Task<AuthResult> LogOut()
    {
        tokenService.DeleteCookies();
        return Task.FromResult(new AuthResult(true, "", ""));
    }

    private AuthResult InvalidCredentials()
    {
        var result = new AuthResult(false, "", "");
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