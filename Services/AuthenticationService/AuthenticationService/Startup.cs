using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AuthenticationService.Database;
using AuthenticationService.Hub;
using AuthenticationService.Model;
using AuthenticationService.Services.Authentication;
using AuthenticationService.Services.Cookie;
using AuthenticationService.Services.EmailSender;
using AuthenticationService.Services.FriendConnection;
using AuthenticationService.Services.User;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using AuthenticationService.Services.PrivateKeyService;

namespace AuthenticationService;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];
        var connectionToPrivateKeys = configuration["ConnectionStringToPrivateKeyDatabase"];
        var issueSign = configuration["IssueSign"];
        var issueAudience = configuration["IssueAudience"];
        var vaultAddress = configuration["HashiCorpAddress"];
        var vaultToken = configuration["HashiCorpToken"];

        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        services.AddHttpContextAccessor();
        services.AddControllers(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        services.AddEndpointsApiExplorer();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserServices, UserServices>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>(); 
        services.AddScoped<IPrivateKeyService, PrivateKeyService>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddGrpc();
        services.AddSingleton<IVaultClient>(vaultClient);

        services.AddDbContext<MainDatabaseContext>(options => options.UseNpgsql(connection));

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
        .AddEntityFrameworkStores<MainDatabaseContext>()
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
            //app.UseSwagger();
            //app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        /* app.UseRefreshTokenMiddleware();
        app.UseJwtRefreshMiddleware(); */

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<FriendRequestHub>("/friend");
            endpoints.MapControllers();
        });

        PopulateDbAndAddRoles.AddRolesAndAdminSync(app, configuration);

        if (!env.IsEnvironment("Test")) return;

        //PopulateDbAndAddRoles.CreateTestUsersSync(app, 5);
    }
}
