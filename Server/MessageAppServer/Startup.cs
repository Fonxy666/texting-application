using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Database;
using Server.Hub;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.Chat;
using Server.Services.Chat.MessageService;
using Server.Services.Chat.RoomService;
using Server.Services.EmailSender;

namespace Server
{
    public class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var connection = configuration["ConnectionString"];
            var issueSign = configuration["IssueSign"];
            var issueAudience = configuration["IssueAudience"];

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

            services.AddSignalR();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
                new Dictionary<string, UserRoomConnection>());
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddDbContext<UsersContext>(options => options.UseSqlServer(connection));
            services.AddDbContext<MessagesContext>(options => options.UseSqlServer(connection));
            services.AddDbContext<RoomsContext>(options => options.UseSqlServer(connection));

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
                        ValidIssuer = issueAudience,
                        ValidAudience = issueAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issueSign))
                    };
                    
                    options.Events = new JwtBearerEvents
                    {
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

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoint =>
            {
                endpoint.MapHub<ChatHub>("/chat");
            });
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            AddRolesAndAdminAndTestUserAsync(app).Wait();
        }

        private async Task AddRolesAndAdminAndTestUserAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var roleList = new List<string> { "User", "Admin" };

            foreach (var role in roleList)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            await CreateAdminIfNotExistAsync(userManager);
            await CreateTestUser(userManager);
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
        
        private async Task CreateTestUser(UserManager<ApplicationUser> userManager)
        {
            const string testEmail = "test@hotmail.com";

            var testInDb = await userManager.FindByEmailAsync(testEmail);
            if (testInDb == null)
            {
                var testUser = new ApplicationUser("-")
                {
                    UserName = "TestUsername",
                    Email = testEmail
                };

                var adminCreated = await userManager.CreateAsync(testUser, "testUserPassword123###");

                if (adminCreated.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
                else
                {
                    Console.WriteLine($"Error creating test user: {string.Join(", ", adminCreated.Errors)}");
                }
            }
        }
    }
}