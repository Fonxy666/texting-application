using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Server.Model;

namespace Server.Services.Authentication;

public class TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private const int ExpirationHours = 2;
    private HttpRequest Request => httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext or Request is null");
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("HttpContext or Response is null");
        
    public string CreateJwtToken(IdentityUser user, string? role)
    {
        var expiration = DateTime.UtcNow.AddHours(ExpirationHours);
        var token = CreateJwtToken(CreateClaims(user, role), CreateSigningCredentials(), expiration);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken CreateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7)
        };
    }

    public void SetRefreshTokenAndUserId(ApplicationUser user)
    {
        var newRefreshToken = CreateRefreshToken();

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
            Response.Cookies.Append("Animation", false.ToString(), new CookieOptions
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