using Microsoft.AspNetCore.Identity;
using Server.Model;

namespace Server.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser user, string? role, bool isTest = false);
    public void SetRefreshToken(ApplicationUser user);
    public void SetCookies(string accessToken, string userId, bool rememberMe);
    public void DeleteCookies();
}