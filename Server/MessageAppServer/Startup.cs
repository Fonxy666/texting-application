﻿using System.Text;
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
using Server.Services.User;

namespace Server
{
    public class Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine(env.IsDevelopment());
            var connection = configuration["ConnectionString"];
            var issueSign = configuration["IssueSign"];
            var issueAudience = configuration["IssueAudience"];
            
            /*var localhost = connection.Split("=")[1].Split(",")[0] == "localhost";
            
            if (localhost)
            {
                if (!DockerContainerHelperClass.IsSqlServerContainerRunning())
                {
                    DockerContainerHelperClass.StopAllRunningContainers();
                    Thread.Sleep(1000);
                    DockerContainerHelperClass.StartSqlServerContainer();
                }
            
                while (!DockerContainerHelperClass.IsSqlServerContainerRunning())
                {
                    Thread.Sleep(1000);
                    if (!DockerContainerHelperClass.IsSqlServerContainerRunning()) continue;
                    if (DockerContainerHelperClass.IsSqlServerContainerRunning())
                    {
                        Thread.Sleep(10000);
                    }
                }
            }*/

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
            services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
                new Dictionary<string, UserRoomConnection>());
            services.AddTransient<IEmailSender, EmailSender>();

            Console.WriteLine(connection);
            services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connection));
            
            services.AddAuthentication(o => {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options => {
                    options.Cookie.Name = "Authorization";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.HttpOnly = true;
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

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
            
            app.Use(async (context, next) =>
            {
                if (context.Request.Cookies.TryGetValue("Authorization", out string? userId))
                {
                    app.UseRefreshTokenMiddleware();
                    app.UseJwtRefreshMiddleware();
                    await next();
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
                endpoints.MapHub<ChatHub>("/chat");
                endpoints.MapControllers();
            });

            AddRolesAndAdminAndTestUserAsync(app).Wait();
        }

        private async Task AddRolesAndAdminAndTestUserAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

            var roleList = new List<string> { "User", "Admin" };

            foreach (var roleName in roleList)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);

                if (!roleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            await CreateAdminIfNotExistAsync(userManager);
            await CreateTestUsers(userManager);
            await CreateTestRoom(roomService);
        }

        private async Task CreateTestRoom(IRoomService roomService)
        {
            if (roomService.GetRoom("test").Result != null)
            {
                return;
            }
            await roomService.RegisterRoomAsync("test", "test");
        }

        private async Task CreateAdminIfNotExistAsync(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = configuration["AdminEmail"];

            var adminInDb = await userManager.FindByEmailAsync(adminEmail!);
            if (adminInDb == null)
            {
                var admin = new ApplicationUser("-")
                {
                    UserName = configuration["AdminUserName"],
                    Email = adminEmail
                };

                var adminCreated = await userManager.CreateAsync(admin, configuration["AdminPassword"]!);

                if (adminCreated.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    Console.WriteLine($"Error creating admin user: {string.Join(", ", adminCreated.Errors)}");
                }
            }
        }

        private async Task CreateTestUsers(UserManager<ApplicationUser> userManager)
        {
            const string testEmail1 = "test1@hotmail.com";
            const string testEmail2 = "test2@hotmail.com";
            const string testEmail3 = "test3@hotmail.com";

            var testInDb1 = await userManager.FindByEmailAsync(testEmail1);
            var testInDb2 = await userManager.FindByEmailAsync(testEmail2);
            var testInDb3 = await userManager.FindByEmailAsync(testEmail3);

            if (testInDb1 == null)
            {
                var testUser = new ApplicationUser("-")
                {
                    Id = new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"),
                    UserName = "TestUsername1",
                    Email = testEmail1,
                    TwoFactorEnabled = true
                };

                var testUserCreated = await userManager.CreateAsync(testUser, "testUserPassword123###");

                if (testUserCreated.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
                else
                {
                    Console.WriteLine($"Error creating test user: {string.Join(", ", testUserCreated.Errors)}");
                }
            }
            if (testInDb2 == null)
            {
                var testUser = new ApplicationUser("-")
                {
                    Id = new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"),
                    UserName = "TestUsername2",
                    Email = testEmail2,
                    TwoFactorEnabled = true
                };

                var testUserCreated = await userManager.CreateAsync(testUser, "testUserPassword123###");

                if (testUserCreated.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
                else
                {
                    Console.WriteLine($"Error creating test user: {string.Join(", ", testUserCreated.Errors)}");
                }
            }
            if (testInDb3 == null)
            {
                var testUser = new ApplicationUser("-")
                {
                    Id = new Guid("995f04da-d4d3-447c-9c69-fab370bca312"),
                    UserName = "TestUsername3",
                    Email = testEmail3,
                    TwoFactorEnabled = false
                };

                var testUserCreated = await userManager.CreateAsync(testUser, "testUserPassword123###");

                if (testUserCreated.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
                else
                {
                    Console.WriteLine($"Error creating test user: {string.Join(", ", testUserCreated.Errors)}");
                }
            }
        }
    }
}