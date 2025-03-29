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

        services.AddGrpcClient<UserAuthenticationService.UserAuthenticationServiceClient>(options =>
        {
            options.Address = new Uri("https://localhost:7100");
        });

        var serviceProvider = services.BuildServiceProvider();
        var authClient = serviceProvider.GetRequiredService<UserAuthenticationService.UserAuthenticationServiceClient>();

        // Make a request to the gRPC server (this is synchronous for testing, you could also make it async)
        MakeGrpcRequest(authClient).Wait();
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

    private async Task MakeGrpcRequest(UserAuthenticationService.UserAuthenticationServiceClient authClient)
    {
        var request = new Empty();  // The Empty message as per your proto definition
        var response = await authClient.GetMessageAsync(request);

        // For testing purposes, log the response message from the server
        Console.WriteLine($"Message from server: {response.Message}");
    }
}
