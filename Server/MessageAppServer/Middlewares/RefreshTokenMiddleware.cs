using Microsoft.AspNetCore.Identity;
using AuthenticationServer.Model;
using AuthenticationServer.Services.Authentication;
using AuthenticationServer.Services.Cookie;

namespace AuthenticationServer.Middlewares;

public class RefreshTokenMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager, ICookieService cookieService)
    {
        if (TokenExpired(context, userManager))
        {
            context.Response.StatusCode = 403;
            cookieService.DeleteCookies();
            return;
        }

        await next(context);
    }

    private bool TokenExpired(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var userId = context.Request.Cookies["UserId"];

        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        if (userManager.FindByIdAsync(userId).Result!.RefreshTokenExpires == null)
        {
            return false;
        }

        var tokenExpiration = userManager.FindByIdAsync(userId).Result!.RefreshTokenExpires;

        return tokenExpiration <= DateTime.UtcNow.AddHours(2);
    }
}

public static class RefreshTokenMiddlewareExtensions
{
    public static void UseRefreshTokenMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<RefreshTokenMiddleware>();
    }
}