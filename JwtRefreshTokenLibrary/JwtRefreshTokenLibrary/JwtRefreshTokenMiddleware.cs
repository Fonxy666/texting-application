using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JwtRefreshMiddlewareLibrary;

public class JwtRefreshTokenMiddleware
{
    public bool ExamineCookies(HttpContext context)
    {
        return context.Request.Cookies["RefreshToken"] != string.Empty &&
               context.Request.Cookies["RefreshToken"] != null &&
               context.Request.Cookies["UserId"] != string.Empty &&
               context.Request.Cookies["UserId"] != null &&
               context.Request.Cookies["Authorization"] == null;
    }

    public bool TokenExpired(HttpContext context)
    {
        var token = context.Request.Cookies["Authorization"];
        var refreshToken = context.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;      //Token decode

            if (jsonToken?.Payload.Exp == null)
            {
                return false;
            }

            var expirationClaim = jsonToken?.Payload.Exp;      //Extract the expiration claims

            if (expirationClaim == null || !long.TryParse(expirationClaim.ToString(), out var expirationTimestamp))
            {
                return true;
            }

            var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp).UtcDateTime;  //Convert to daytime
            var isExpired = expirationDateTime < DateTime.UtcNow.AddHours(2);

            return isExpired;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public string? GetRoleFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (handler.ReadToken(token) is JwtSecurityToken jsonToken)
            {
                var roleClaims = jsonToken.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                switch (roleClaims.Count)
                {
                    case 1:
                        return roleClaims[0];
                    case > 1:
                        return roleClaims[0];
                }
            }
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Invalid token format!");
        }
        return null;
    }
}
