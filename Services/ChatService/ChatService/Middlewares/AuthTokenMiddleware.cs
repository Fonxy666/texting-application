using System.Security.Claims;

namespace ChatService.Middlewares;

public class AuthTokenMiddleware(RequestDelegate next, GrpcUserService.GrpcUserServiceClient grpcClient)
{
    public async Task InvokeAsync(HttpContext context)
    {
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
