using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Services.Authentication;

namespace Server.Middlewares;

public class JwtRefreshMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager)
    {
        if (ExamineCookies(context))
        {
            await SetNewJwtToken(context, tokenService, userManager);
        }
        if (TokenExpired(context))
        {
            await RefreshToken(context, tokenService, userManager);
        }

        await next(context);
    }

    private async Task SetNewJwtToken(HttpContext context, ITokenService tokenService,
        UserManager<ApplicationUser> userManager)
    {
        var userId = context.Request.Cookies["UserId"];
        var user = userManager.Users.FirstOrDefault(user => user.Id == userId);
        var newToken = tokenService.CreateJwtToken(user!, "User");

        await tokenService.SetJwtToken(newToken);
    }

    private bool ExamineCookies(HttpContext context)
    {
        return context.Request.Cookies["RefreshToken"] != string.Empty &&
               context.Request.Cookies["RefreshToken"] != null &&
               context.Request.Cookies["UserId"] != string.Empty &&
               context.Request.Cookies["UserId"] != null &&
               context.Request.Cookies["Authorization"] == null;
    }

    private bool TokenExpired(HttpContext context)
    {
        var token = context.Request.Cookies["Authorization"];

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;      //Token decode

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

    private async Task RefreshToken(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(context.Request.Cookies["UserId"]!);

        if (user != null && user.RefreshToken == context.Request.Cookies["RefreshToken"])
        {
            var token = context.Request.Cookies["Authorization"];
            var role = GetRoleFromToken(token!);
            var newToken = tokenService.CreateJwtToken(user, role);

            await tokenService.SetJwtToken(newToken);
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
    }
    
    public static string? GetRoleFromToken(string token)
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

public static class JwtRefreshMiddlewareExtensions
{
    public static void UseJwtRefreshMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<JwtRefreshMiddleware>();
    }
}