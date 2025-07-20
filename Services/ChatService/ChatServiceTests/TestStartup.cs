using System.Text;
using ChatService;
using ChatService.Database;
using ChatService.Hub;
using ChatService.Model;
using ChatService.Repository.BaseRepository;
using ChatService.Repository.MessageRepository;
using ChatService.Repository.RoomRepository;
using ChatService.Services.Chat.GrpcService;
using ChatService.Services.Chat.MessageService;
using ChatService.Services.Chat.RoomService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Textinger.Shared.JwtRefreshTokenValidation;

namespace ChatServiceTests;

public class TestStartup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["TestConnectionString"];
        var issueSign = configuration["IssueSign"];
        var issueAudience = configuration["IssueAudience"];

        services.AddHttpContextAccessor();
        services.AddControllers()
            .AddApplicationPart(typeof(ChatService.Controllers.ChatController).Assembly)
            .AddApplicationPart(typeof(ChatService.Controllers.MessageController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddEndpointsApiExplorer();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddSingleton<IJwtRefreshTokenValidator, JwtRefreshTokenValidator>();
        services.AddScoped<IUserGrpcService, FakeUserGrpcService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IBaseDatabaseRepository, BaseDatabaseRepository>();
        services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
            new Dictionary<string, UserRoomConnection>());

        services.AddDbContext<ChatContext>(options =>
            options.UseNpgsql(connection, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

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
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chat");
            endpoints.MapControllers();
        });
    }
}