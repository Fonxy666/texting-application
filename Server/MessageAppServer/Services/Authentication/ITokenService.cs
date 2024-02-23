using Microsoft.AspNetCore.Identity;

namespace Server.Services.Authentication;

public interface ITokenService
{
    public string CreateToken(IdentityUser user, string? role, bool isTest = false);
    public void SetCookies(string accessToken, string id, bool rememberMe);
    public void DeleteCookies();
}