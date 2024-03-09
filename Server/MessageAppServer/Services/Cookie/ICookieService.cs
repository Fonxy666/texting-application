using Server.Model;

namespace Server.Services.Cookie;

public interface ICookieService
{
    public void SetRefreshTokenAndUserId(ApplicationUser user);
    public void SetAnimateAndAnonymous();
    public void ChangeAnimation();
    public void ChangeUserAnonymous();
    public Task<bool> SetJwtToken(string accessToken);
    public void DeleteCookies();
}