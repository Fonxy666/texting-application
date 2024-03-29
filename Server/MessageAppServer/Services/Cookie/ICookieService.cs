using Server.Model;

namespace Server.Services.Cookie;

public interface ICookieService
{
    public void SetUserId(string userId);
    public void SetRefreshToken(ApplicationUser user);
    public void SetAnimateAndAnonymous();
    public void ChangeAnimation();
    public void ChangeUserAnonymous();
    public Task<bool> SetJwtToken(string accessToken, bool rememberMe);
    public void DeleteCookies();
}