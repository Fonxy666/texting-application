﻿using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Database;
using Server.Services.Authentication;

namespace Server;

public class Startup(IConfiguration configuration)
{
    #region ConfigureServices
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
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
        
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        var connection = configuration["ConnectionString"];
        var iS = configuration["IssueSign"];
        var iA = configuration["IssueAudience"];
        
        services.AddDbContext<UsersContext>(options => options.UseSqlServer(connection));
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = iA,
                    ValidAudience = iA,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(iS))
                };
            });
        
        services.AddIdentityCore<IdentityUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<UsersContext>(); 
    }
    #endregion

    #region Configure
    public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:4200");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
            }
            else
            {
                await next();
            }
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        await AddRolesAndAdmin(app);
    }
    #endregion

    #region Admin

    private async Task AddRolesAndAdmin(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var roleList = new List<string> { "User", "Admin" };
        
        foreach (var role in roleList)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        await CreateAdminIfNotExist(userManager);
    }

    private async Task CreateAdminIfNotExist(UserManager<IdentityUser> userManager)
    {
        var adminEmail = configuration["AdminEmail"];

        var adminInDb = await userManager.FindByEmailAsync(adminEmail);
        if (adminInDb == null)
        {
            var admin = new IdentityUser { UserName = configuration["AdminUserName"], Email = adminEmail };
            var adminCreated = await userManager.CreateAsync(admin, configuration["AdminPassword"]);

            if (adminCreated.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                Console.WriteLine("Admin user created successfully.");
            }
            else
            {
                Console.WriteLine($"Error creating admin user: {string.Join(", ", adminCreated.Errors)}");
            }
        }
    }
    #endregion
}