using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatServiceTests;

public static class FakeLogin
{
    private static JwtSecurityToken TestJwtSecurityToken(string userId, IConfiguration configuration, string role = "User")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };

        var expires = DateTime.UtcNow.AddDays(7);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["IssueSign"]!)),
            SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: configuration["IssueAudience"],
            audience: configuration["IssueAudience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials
        );
    }

    public static void FakeLoginToClient(HttpClient client, Guid userId, IConfiguration config)
    {
        client.DefaultRequestHeaders.Clear();
        
        var jwt = TestJwtSecurityToken(userId.ToString(), config);
        var jwtString = new JwtSecurityTokenHandler().WriteToken(jwt);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtString);
        client.DefaultRequestHeaders.Add("Cookie", $"UserId={userId}");
    }
}