using Server.Model;
using Server.Services.Authentication;

namespace Server.Services.Cookie;

public class CookieService(IHttpContextAccessor httpContextAccessor, ITokenService tokenService) : ICookieService
{
    private HttpRequest Request => httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext or Request is null");
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("HttpContext or Response is null");
    
    private const int ExpirationHours = 3;

    public void SetUserId(Guid userId, bool rememberMe)
    {
        Response.Cookies.Append("UserId", userId.ToString(), new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = false,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = rememberMe? DateTime.UtcNow.AddDays(7) : null
        });
    }

    public void SetRefreshToken(ApplicationUser user)
    {
        var newRefreshToken = tokenService.CreateRefreshToken();

        Response.Cookies.Append("RefreshToken", newRefreshToken.Token!, new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = newRefreshToken.Expires
        });
        
        user.SetRefreshToken(newRefreshToken.Token);
        user.SetRefreshTokenCreated(newRefreshToken.Created);
        user.SetRefreshTokenExpires(newRefreshToken.Expires);
    }

    public void SetAnimateAndAnonymous(bool rememberMe)
    {
        if (Request.Cookies["Animation"] == null)
        {
            Response.Cookies.Append("Animation", true.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = rememberMe? DateTime.UtcNow.AddYears(2) : null
            });
        }

        if (Request.Cookies["Anonymous"] == null)
        {
            Response.Cookies.Append("Anonymous", false.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = rememberMe? DateTime.UtcNow.AddYears(2) : null
            });
        }
    }

    public void ChangeAnimation()
    {
        var rememberMe = Request.Cookies["RememberMe"] == "True";
        Response.Cookies.Append("Animation",
            Request.Cookies["Animation"] == "False" ? true.ToString() : false.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = rememberMe? DateTime.UtcNow.AddYears(2) : null
            });
    }

    public void ChangeUserAnonymous()
    {
        var rememberMe = Request.Cookies["RememberMe"] == "True";
        Response.Cookies.Append("Anonymous",
            Request.Cookies["Anonymous"] == "False" ? true.ToString() : false.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = rememberMe? DateTime.UtcNow.AddYears(2) : null
            });
    }

    public Task<bool> SetJwtToken(string accessToken, bool rememberMe)
    {
        Response.Cookies.Append("Authorization", accessToken, new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = rememberMe? DateTimeOffset.UtcNow.AddHours(ExpirationHours) : null
        });

        return Task.FromResult(true);
    }

    public void DeleteCookies()
    {
        var cookieOptions = new CookieOptions
        {
            SameSite = SameSiteMode.None,
            Secure = true
        };
        
        Response.Cookies.Delete("Authorization", cookieOptions);
        Response.Cookies.Delete("UserId", cookieOptions);
        Response.Cookies.Delete("RefreshToken", cookieOptions);
        Response.Cookies.Delete("RememberMe", cookieOptions);
        Response.Cookies.Delete("Anonymous", cookieOptions);
    }

    public void SetRememberMeCookie(bool rememberMe)
    {
        Response.Cookies.Append("RememberMe", rememberMe.ToString(), new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = rememberMe? DateTime.UtcNow.AddYears(2) : null
        });
    }

    public void SetPublicKey(bool rememberMe, string publicKey)
    {
        Response.Cookies.Append("PublicKey", publicKey, new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = false,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = rememberMe? DateTime.UtcNow.AddDays(7) : null
        });
    }
}