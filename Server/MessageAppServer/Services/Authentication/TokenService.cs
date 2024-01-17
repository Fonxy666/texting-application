using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Server.Services.Authentication;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private const int ExpirationMinutes = 30;
        
    public string CreateToken(IdentityUser user, string? role, bool isTest = false)
    {
        var expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
         var token = CreateJwtToken(CreateClaims(user, role), CreateSigningCredentials(), expiration);
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials, DateTime expiration)
    {
        return new JwtSecurityToken(
            "haha",
            "haha",
            claims,
            expires: expiration,
            signingCredentials: credentials
        );
    }

    private List<Claim> CreateClaims(IdentityUser user, string? role)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "haha"),
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
        return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("haha")), SecurityAlgorithms.HmacSha256);
    }
}