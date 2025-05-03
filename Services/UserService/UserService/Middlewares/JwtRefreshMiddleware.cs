using Microsoft.AspNetCore.Identity;
using UserService.Services.Cookie;
using UserService.Services.Authentication;
using UserService.Models;
using Textinger.Shared.JwtRefreshTokenValidator;

namespace UserService.Middlewares;

public class JwtRefreshMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager, ICookieService cookieService, JwtRefreshTokenMiddleware jwtMiddleware)
    {
        if (jwtMiddleware.ExamineCookies(context))
        {
            await SetNewJwtToken(context, tokenService, userManager, cookieService);
        }

        if (jwtMiddleware.TokenExpired(context))
        {
            await RefreshToken(context, tokenService, userManager, cookieService, jwtMiddleware);
        }

        await next(context);
    }

    private static async Task SetNewJwtToken(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager, ICookieService cookieService)
    {
        var rememberMe = context.Request.Cookies["RememberMe"] == "True";
        
        var userId = context.Request.Cookies["UserId"];
        var user = userManager.Users.FirstOrDefault(user => user.Id.ToString() == userId);
        var newToken = tokenService.CreateJwtToken(user!, "User", rememberMe);

        await cookieService.SetJwtToken(newToken, rememberMe);
    }

    private async Task RefreshToken(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager, ICookieService cookieService, JwtRefreshTokenMiddleware jwtMiddleware)
    {
        var user = await userManager.FindByIdAsync(context.Request.Cookies["UserId"]!);
        var rememberMe = context.Request.Cookies["RememberMe"] == "True";

        if (user != null && user.RefreshToken == context.Request.Cookies["RefreshToken"])
        {
            var token = context.Request.Cookies["Authorization"];
            var role = jwtMiddleware.GetRoleFromToken(token!);
            var newToken = tokenService.CreateJwtToken(user, role, rememberMe);

            await cookieService.SetJwtToken(newToken, rememberMe);
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
    }
}

public static class JwtRefreshMiddlewareExtensions
{
    public static void UseJwtRefreshMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<JwtRefreshMiddleware>();
    }
}