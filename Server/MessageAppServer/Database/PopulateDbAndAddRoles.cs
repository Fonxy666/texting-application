using Microsoft.AspNetCore.Identity;
using Server.Model;
using Server.Model.Requests.User;
using Server.Services.Chat.RoomService;
using Server.Services.FriendConnection;
using Server.Services.User;

namespace Server.Database;

public static class PopulateDbAndAddRoles
{
    public static async Task AddRolesAndAdmin(IApplicationBuilder app, IConfiguration configuration)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roleList = new List<string> { "User", "Admin" };

        foreach (var roleName in roleList)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        await CreateAdminIfNotExistAsync(userManager, configuration);
    }

    public static async Task CreateTestRoom(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

        try
        {
            var existingRoom = await roomService.GetRoomById(new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a9"));
            if (existingRoom != null)
            {
                return;
            }

            await roomService.RegisterRoomAsync("test", "test", new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a9"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating or checking room: {ex.Message}");
        }
    }
    
    private static async Task CreateAdminIfNotExistAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
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
    
    public static async Task CreateTestUsers(IApplicationBuilder app, int numberOfTestUsers)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        for (var i = 1; i <= numberOfTestUsers; i++)
        {
            var testEmail = $"test{i}@hotmail.com";
            var testInDb = await userManager.FindByEmailAsync(testEmail);

            if (testInDb != null) continue;
            
            var testUser = new ApplicationUser("-")
            {
                Id = i switch
                {
                    1 => new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"),
                    2 => new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"),
                    3 => new Guid("995f04da-d4d3-447c-9c69-fab370bca312"),
                    _ => Guid.NewGuid()
                },
                
                UserName = $"TestUsername{i}",
                Email = testEmail,
                TwoFactorEnabled = i != 3
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