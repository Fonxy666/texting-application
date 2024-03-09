using Microsoft.AspNetCore.Identity;
using Server.Model;

namespace Server.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser user, string? role);
    public void SetRefreshTokenAndUserId(ApplicationUser user);
    public void SetAnimateAndAnonymous();
    public void ChangeAnimation();
    public void ChangeUserAnonymous();
    public Task<bool> SetJwtToken(string accessToken);
    public void DeleteCookies();
}