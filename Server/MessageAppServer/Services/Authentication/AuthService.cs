using Microsoft.AspNetCore.Identity;
using Server.Model;

namespace Server.Services.Authentication;

public class AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string username, string password, string role, string phoneNumber, string image)
    {
        var user = new ApplicationUser(image) { UserName = username, Email = email, PhoneNumber = phoneNumber, PhoneNumberConfirmed = true };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return FailedRegistration(result, email, username);
        }

        await userManager.AddToRoleAsync(user, role);
        return new AuthResult(true, email, username, "");
    }

    private static AuthResult FailedRegistration(IdentityResult result, string email, string username)
    {
        var authenticationResult = new AuthResult(false, email, username, "");

        foreach (var identityError in result.Errors)
        {
            authenticationResult.ErrorMessages.Add(identityError.Code, identityError.Description);
        }

        return authenticationResult;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var managedUser = await userManager.FindByNameAsync(username);

        if (managedUser == null)
        {
            return InvalidLogin();
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, password);

        if (!isPasswordValid)
        {
            return InvalidLogin();
        }

        var roles = await userManager.GetRolesAsync(managedUser);
        var accessToken = tokenService.CreateToken(managedUser, roles[0]);

        return new AuthResult(true, managedUser.Email!, managedUser.UserName!, accessToken);
    }

    private AuthResult InvalidLogin()
    {
        var result = new AuthResult(false, "", "", "");
        result.ErrorMessages.Add("Bad credentials", "Invalid email");
        return result;
    }
}