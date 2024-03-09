using Server.Model;
using Server.Services.Authentication;

namespace Server.Services.Cookie;

public class CookieService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ITokenService tokenService) : ICookieService
{
    private HttpRequest Request => httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext or Request is null");
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("HttpContext or Response is null");
    private const int ExpirationHours = 2;
    
    public void SetRefreshTokenAndUserId(ApplicationUser user)
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
        
        user.RefreshToken = newRefreshToken.Token;
        user.RefreshTokenCreated = newRefreshToken.Created;
        user.RefreshTokenExpires = newRefreshToken.Expires;
        
        Response.Cookies.Append("UserId", user.Id, new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = false,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = newRefreshToken.Expires
        });
    }
    
    public void SetAnimateAndAnonymous()
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
                Expires = DateTime.UtcNow.AddYears(2)
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
                Expires = DateTime.UtcNow.AddYears(2)
            });
        }
    }

    public void ChangeAnimation()
    {
        Response.Cookies.Append("Animation",
            Request.Cookies["Animation"] == "False" ? true.ToString() : false.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddYears(2)
            });
    }

    public void ChangeUserAnonymous()
    {
        Response.Cookies.Append("Anonymous",
            Request.Cookies["Anonymous"] == "False" ? true.ToString() : false.ToString(), new CookieOptions
            {
                Domain = Request.Host.Host,
                HttpOnly = false,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddYears(2)
            });
    }

    public Task<bool> SetJwtToken(string accessToken)
    {
        Response.Cookies.Append("Authorization", accessToken, new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddHours(ExpirationHours)
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
    }
}