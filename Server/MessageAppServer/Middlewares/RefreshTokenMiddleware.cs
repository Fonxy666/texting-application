using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Services.Authentication;

namespace Server.Middlewares;

public class RefreshTokenMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ITokenService tokenService, UserManager<ApplicationUser> userManager)
    {
        if (TokenExpired(context, userManager))
        {
            Console.WriteLine($"TOKENEXPIRED");
            context.Response.StatusCode = 403;
            tokenService.DeleteCookies();
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

        var tokenExpiration = userManager.FindByIdAsync(userId).Result!.RefreshTokenExpires;

        return tokenExpiration <= DateTime.UtcNow.AddHours(1);
    }
}

public static class RefreshTokenMiddlewareExtensions
{
    public static void UseRefreshTokenMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<RefreshTokenMiddleware>();
    }
}