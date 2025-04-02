using ChatService.Database;
using ChatService.Hub;
using ChatService.Model;
using ChatService.Services.Chat.MessageService;
using ChatService.Services.Chat.RoomService;
using Microsoft.EntityFrameworkCore;

namespace ChatService;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = configuration["ConnectionString"];

        services.AddHttpContextAccessor();
        services.AddControllers(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        services.AddEndpointsApiExplorer();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

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

        services.AddGrpcClient<GrpcUserService.GrpcUserServiceClient>(options =>
        {
            options.Address = new Uri("https://localhost:7100");
        });

        var serviceProvider = services.BuildServiceProvider();
        var userClient = serviceProvider.GetRequiredService<GrpcUserService.GrpcUserServiceClient>();

        // Make a request to the gRPC server (this is synchronous for testing, you could also make it async)
        MakeGrpcRequest(userClient).Wait();
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

        app.UseSession();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chat");
            endpoints.MapControllers();
        });

        if (!env.IsEnvironment("Test")) return;
    }

    private async Task MakeGrpcRequest(GrpcUserService.GrpcUserServiceClient authClient)
    {
        var userIdRequest = new GuidRequest { Guid = "3171cd1b-0c5a-4a4f-b3f7-90d1d2e8cf65" };
        var userIdResponse = await authClient.UserExistingAsync(userIdRequest);

        Console.WriteLine($"User exists: {userIdResponse.Success}");
    }
}
