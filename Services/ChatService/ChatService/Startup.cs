using System.Text;
using ChatService.Database;
using ChatService.Hub;
using ChatService.Middlewares;
using ChatService.Model;
using ChatService.Services.Chat.GrpcService;
using ChatService.Services.Chat.MessageService;
using ChatService.Services.Chat.RoomService;
using JwtRefreshMiddlewareLibrary;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ChatService;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];
        var issueSign = configuration["IssueSign"];
        var issueAudience = configuration["IssueAudience"];
        var grpcUri = new Uri("https://localhost:7100");

        services.AddHttpContextAccessor();
        services.AddControllers(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        services.AddEndpointsApiExplorer();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddSingleton<JwtRefreshTokenMiddleware>();
        services.AddScoped<IUserGrpcService, UserGrpcService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
            new Dictionary<string, UserRoomConnection>());

        services.AddDbContext<ChatContext>(options => options.UseNpgsql(connection));

        services.AddDistributedMemoryCache();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddAuthentication(o => {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
            .AddCookie(options =>
            {
                options.Cookie.Name = "Authorization";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.HttpOnly = true;
                options.LoginPath = string.Empty;
                options.AccessDeniedPath = string.Empty;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issueAudience,
                    ValidAudience = issueAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issueSign)),
                    RequireExpirationTime = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["Authorization"];
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation($"Token validation successful for user: {context.Principal.Identity?.Name}.", context.Principal.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogError($"Authentication failed: {context.Exception.Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddGrpcClient<GrpcUserService.GrpcUserServiceClient>(options =>
        {
            options.Address = grpcUri;
        });

        services.AddGrpcClient<GrpcAuthService.GrpcAuthServiceClient>(options =>
        {
            options.Address = grpcUri;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthTokenMiddleware();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chat");
            endpoints.MapControllers();
        });

        if (!env.IsEnvironment("Test")) return;
    }
}
