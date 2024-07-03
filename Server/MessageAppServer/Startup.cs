using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Database;
using Server.Hub;
using Server.Middlewares;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.Chat.MessageService;
using Server.Services.Chat.RoomService;
using Server.Services.Cookie;
using Server.Services.EmailSender;
using Server.Services.FriendConnection;
using Server.Services.User;

namespace Server;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];
        var issueSign = configuration["IssueSign"];
        var issueAudience = configuration["IssueAudience"];

        services.AddHttpContextAccessor();
        services.AddControllers(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IUserServices, UserServices>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>();
        services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
            new Dictionary<string, UserRoomConnection>());
        services.AddTransient<IEmailSender, EmailSender>();

        services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connection));
        
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
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration["GoogleClientId"]!;
                options.ClientSecret = configuration["GoogleClientSecret"]!;
            })
            .AddFacebook("Facebook", options =>
            {
                options.ClientId = configuration["FacebookClientId"]!;
                options.ClientSecret = configuration["FacebookClientSecret"]!;
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
                        logger.LogInformation("Token validation successful for user: {username}", context.Principal.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogError("Authentication failed: {exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<DatabaseContext>()
        .AddDefaultTokenProviders();
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        
        app.UseCors(builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
        
        app.UseRefreshTokenMiddleware();
        app.UseJwtRefreshMiddleware();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chat");
            endpoints.MapHub<FriendRequestHub>("/friend");
            endpoints.MapControllers();
        });
        
        PopulateDbAndAddRoles.AddRolesAndAdmin(app, configuration);
        
        if (!env.IsEnvironment("Test")) return;
            
        PopulateDbAndAddRoles.CreateTestUsers(app, 5).Wait();
        PopulateDbAndAddRoles.CreateTestRoom(app).Wait();
    }
}
