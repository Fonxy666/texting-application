using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using AuthenticationServer.Model;

namespace AuthenticationServer.Services.Authentication;

public class TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    public const int ExpirationHours = 2;
    private HttpRequest Request => httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext or Request is null");
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response ?? throw new InvalidOperationException("HttpContext or Response is null");
        
    public string CreateJwtToken(IdentityUser<Guid> user, string? role, bool rememberMe)
    {
        var expiration = rememberMe? DateTime.UtcNow.AddHours(3) : (DateTime?)null;
        var token = CreateJwtToken(CreateClaims(user, role), CreateSigningCredentials(), expiration);

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public RefreshToken CreateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7)
        };
    }

    private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials, DateTime? expiration)
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

    public List<Claim> CreateClaims(IdentityUser<Guid> user, string? role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
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

    private SigningCredentials CreateSigningCredentials()
    {
        return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["IssueSign"]!)), SecurityAlgorithms.HmacSha256);
    }
}