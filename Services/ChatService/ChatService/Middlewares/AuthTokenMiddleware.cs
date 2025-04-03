using Grpc.Net.Client;
using JwtRefreshMiddlewareLibrary;

namespace ChatService.Middlewares;

public class AuthTokenMiddleware(RequestDelegate next, GrpcUserService.GrpcUserServiceClient grpcClient, JwtRefreshTokenMiddleware jwtMiddleware)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var grpcUri = new Uri("https://localhost:7100");
        var userId = context.Request.Cookies["UserId"];
        var rememberMe = context.Request.Cookies["RememberMe"] == "True";

        if (userId == null || rememberMe == false)
        {
            return;
        }

        if (jwtMiddleware.ExamineCookies(context))
        {
            var newToken = await GetNewJwtToken(grpcUri, userId, rememberMe);
            context.Response.Cookies.Append("Authorization", newToken, new CookieOptions
            {
                Domain = context.Request.Host.Host,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Secure = true,
                Expires = rememberMe ? DateTimeOffset.UtcNow.AddHours(2) : null
            });
        }

        if (jwtMiddleware.TokenExpired(context))
        {
            // need new refresh token
        }

        var userIdResponse = await grpcClient.UserExistingAsync(new UserIdRequest { Id = userId });
        Console.WriteLine($"User exists: {userIdResponse.Success}");

        await next(context);
    }

    private async Task<string> GetNewJwtToken(Uri grpcUri, string userId, bool rememberMe)
    {
        using var channel = GrpcChannel.ForAddress(grpcUri);
        var client = new GrpcAuthService.GrpcAuthServiceClient(channel);

        var request = new GrpcNewJwtRequest
        {
            UserId = userId,
            Remember = rememberMe
        };

        try
        {
            var response = await client.NewJwtTokenAsync(request);
            return response.JwtToken;
        }
        catch (Exception ex)
        {
            return $"gRPC Error: {ex.Message}";
        }
    }
}

public static class AuthTokenMiddlewareExtensions
{
    public static void UseAuthTokenMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuthTokenMiddleware>();
    }
}
