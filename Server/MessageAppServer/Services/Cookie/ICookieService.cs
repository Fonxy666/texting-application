using Server.Model;

namespace Server.Services.Cookie;

public interface ICookieService
{
    public void SetUserId(Guid userId, bool rememberMe);
    public void SetRefreshToken(ApplicationUser user);
    public void SetAnimateAndAnonymous(bool rememberMe);
    public void ChangeAnimation();
    public void ChangeUserAnonymous();
    public Task<bool> SetJwtToken(string accessToken, bool rememberMe);
    public void DeleteCookies();
    public void SetRememberMeCookie(bool rememberMe);
    public void SetPublicKey(bool rememberMe, string publicKey);
}