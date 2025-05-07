using UserService.Models;
using UserService.Services.Cookie;

namespace UserServiceTests;

public class FakeCookieService : ICookieService
{
    public void SetRefreshToken(ApplicationUser user) { }
    public void ChangeUserAnonymous() { }
    public void DeleteCookies() { }
    public Task<bool> SetJwtToken(string token, bool rememberMe) => Task.FromResult(true);
    public void SetRememberMeCookie(bool rememberMe) { }
    public void SetPublicKey(bool rememberMe, string publicKey) { }
    public void SetUserId(Guid userId, bool rememberMe) { }
    public void SetAnimateAndAnonymous(bool rememberMe) { }
    public void ChangeAnimation() { }
}