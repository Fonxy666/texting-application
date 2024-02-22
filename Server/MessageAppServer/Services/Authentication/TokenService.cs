using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Server.Services.Authentication;

public class TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private const int ExpirationMinutes = 30;
    private HttpRequest Request => httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext or Request is null");
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("HttpContext or Response is null");
        
    public string CreateToken(IdentityUser user, string? role, bool isTest = false)
    {
        var expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
        var token = CreateJwtToken(CreateClaims(user, role), CreateSigningCredentials(), expiration);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
    
    public void SetCookies(string accessToken, string id, bool rememberMe)
    {
        Response.Cookies.Append("Authorization", accessToken, GetCookieOptions(true, rememberMe));
        
        Response.Cookies.Append("UserId", id, GetCookieOptions(false, rememberMe));
    }

    private CookieOptions GetCookieOptions(bool httpOnly, bool rememberMe)
    {
        var expireTime = DateTimeOffset.UtcNow.AddHours(1);
        var extendedTime = expireTime.AddMinutes(5);
        
        return new CookieOptions
        {
            Domain = Request.Host.Host,
            HttpOnly = httpOnly,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Secure = true,
            Expires = rememberMe? expireTime : null,
            MaxAge = TimeSpan.FromSeconds((extendedTime-DateTime.UtcNow).TotalSeconds)
        };
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
    }

    private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials, DateTime expiration)
    {
        return new JwtSecurityToken(
            issuer: configuration["IssueAudience"],
            audience: configuration["IssueAudience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiration,
            signingCredentials: credentials
        );
    }

    public List<Claim> CreateClaims(IdentityUser user, string? role)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, configuration["IssueAudience"]!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.NameIdentifier, user.Id)
            };

            if (!string.IsNullOrEmpty(user.UserName))
            {
                claims.Add(new(ClaimTypes.Name, user.UserName));
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new(ClaimTypes.Email, user.Email));
            }

            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private SigningCredentials CreateSigningCredentials()
    {
        return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["IssueSign"]!)), SecurityAlgorithms.HmacSha256);
    }
}