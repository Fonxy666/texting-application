using System.Security.Claims;
using JwtRefreshMiddlewareLibrary;

namespace ChatService.Middlewares;

public class AuthTokenMiddleware(RequestDelegate next, GrpcUserService.GrpcUserServiceClient grpcClient, JwtRefreshTokenMiddleware jwtMiddleware)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (jwtMiddleware.ExamineCookies(context))
        {
            // need new jwt token
        }

        if (jwtMiddleware.TokenExpired(context))
        {
            // need new refresh token
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return;
        }

        var userIdResponse = await grpcClient.UserExistingAsync(new UserIdRequest { Guid = userId });
        Console.WriteLine($"User exists: {userIdResponse.Success}");

        await next(context);
    }
}

public static class AuthTokenMiddlewareExtensions
{
    public static void UseAuthTokenMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuthTokenMiddleware>();
    }
}
