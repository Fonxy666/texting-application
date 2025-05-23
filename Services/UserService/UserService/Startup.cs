using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Database;
using UserService.Hub;
using UserService.Models;
using UserService.Services.Authentication;
using UserService.Services.Cookie;
using UserService.Services.EmailSender;
using UserService.Services.User;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using UserService.Services.PrivateKeyFolder;
using UserService.Services.gRPCServices;
using UserService.Middlewares;
using UserService.Services.EncryptedSymmetricKeyService;
using UserService.Filters;
using UserService.Services.FriendConnectionService;
using Textinger.Shared.JwtRefreshTokenValidation;
using UserService.Services.MediaService;

namespace UserService;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];
        var issueSign = configuration["IssueSign"];
        var issueAudience = configuration["IssueAudience"];
        var vaultAddress = configuration["HashiCorpAddress"];
        var vaultToken = configuration["HashiCorpToken"];

        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        services.AddHttpContextAccessor();
        services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            options.Filters.Add<ValidateModelAttribute>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

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

        services.AddSingleton<IJwtRefreshTokenValidator, JwtRefreshTokenValidator>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IFriendConnectionService, FriendConnectionService>(); 
        services.AddScoped<IPrivateKeyService, PrivateKeyService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ISymmetricKeyService, SymmetricKeyService>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddGrpc();
        services.AddSingleton<IVaultClient>(vaultClient);

        services.AddDbContext<MainDatabaseContext>(options =>
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

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration, IHostApplicationLifetime lifetime)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseRefreshTokenMiddleware();
        app.UseJwtRefreshMiddleware();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<FriendRequestHub>("/friend");
            endpoints.MapControllers();
            endpoints.MapGrpcService<UserGrpcService>();
            endpoints.MapGrpcService<AuthGrpcService>();
        });

        PopulateDbAndAddRoles.AddRolesAndAdminSync(app, configuration);
    }
}
