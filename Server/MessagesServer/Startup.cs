using Microsoft.OpenApi.Models;
using MessagesServer.Database;
using MessagesServer.Hub;
using MessagesServer.Model;
using MessagesServer.Services.Chat.MessageService;
using MessagesServer.Services.Chat.RoomService;

namespace MessagesServer;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];
        var connectionToPrivateKeys = configuration["ConnectionStringToPrivateKeyDatabase"];
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

        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddSingleton<IDictionary<string, UserRoomConnection>>(opt =>
            new Dictionary<string, UserRoomConnection>());

        services.AddDbContext<MainDatabaseContext>(options => options.UseSqlServer(connection));

        services.AddDistributedMemoryCache();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
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

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chat");
            endpoints.MapControllers();
        });

        if (!env.IsEnvironment("Test")) return;

        PopulateDbAndAddRoles.CreateTestUsersSync(app, 5);
        PopulateDbAndAddRoles.CreateTestRoomSync(app);
    }
}
