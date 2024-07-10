using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Server.Model;
using Server.Services.Chat.RoomService;

namespace Server.Database;

public static class PopulateDbAndAddRoles
{
    private static readonly object LockObject = new();

    public static void AddRolesAndAdminSync(IApplicationBuilder app, IConfiguration configuration)
    {
        lock (LockObject)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var roleList = new List<string> { "User", "Admin" };

            foreach (var roleName in roleList)
            {
                var roleExists = roleManager.RoleExistsAsync(roleName).Result;

                if (!roleExists)
                {
                    var roleResult = roleManager.CreateAsync(new IdentityRole<Guid>(roleName)).Result;

                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"Successfully created role {roleName}.");
                    }
                    else
                    {
                        Console.WriteLine($"Error creating role {roleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            CreateAdminIfNotExistSync(userManager, configuration);
        }
    }

    private static void CreateAdminIfNotExistSync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        lock (LockObject)
        {
            var adminEmail = configuration["AdminEmail"];

            var adminInDb = userManager.FindByEmailAsync(adminEmail!).Result;
            if (adminInDb == null)
            {
                var admin = new ApplicationUser("-")
                {
                    UserName = configuration["AdminUserName"],
                    Email = adminEmail
                };

                admin.SetPublicKey(new JsonWebKey());
                var adminCreated = userManager.CreateAsync(admin, configuration["AdminPassword"]!).Result;

                if (adminCreated.Succeeded)
                {
                    var roleResult = userManager.AddToRoleAsync(admin, "Admin").Result;

                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"Successfully created admin user {admin.UserName} and added to Admin role.");
                    }
                    else
                    {
                        Console.WriteLine($"Error adding admin user to role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Error creating admin user: {string.Join(", ", adminCreated.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists.");
            }
        }
    }

    public static void CreateTestUsersSync(IApplicationBuilder app, int numberOfTestUsers)
    {
        lock (LockObject)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            for (var i = 1; i <= numberOfTestUsers; i++)
            {
                var testEmail = $"test{i}@hotmail.com";
                var testUsername = $"TestUsername{i}";

                Console.WriteLine($"Creating test user {i}: {testUsername} ({testEmail})");

                var testInDb = userManager.FindByEmailAsync(testEmail).Result;
                var usernameInDb = userManager.FindByNameAsync(testUsername).Result;

                if (testInDb != null || usernameInDb != null)
                {
                    Console.WriteLine($"User with email {testEmail} or username {testUsername} already exists.");
                    continue;
                }

                var testUser = new ApplicationUser("-")
                {
                    Id = i switch
                    {
                        1 => new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"),
                        2 => new Guid("10f96e12-e245-420a-8bad-b61fb21c4b2d"),
                        3 => new Guid("995f04da-d4d3-447c-9c69-fab370bca312"),
                        _ => Guid.NewGuid()
                    },
                    UserName = testUsername,
                    Email = testEmail,
                    TwoFactorEnabled = i != 3
                };

                var testUserCreated = userManager.CreateAsync(testUser, "testUserPassword123###").Result;

                if (testUserCreated.Succeeded)
                {
                    var roleResult = userManager.AddToRoleAsync(testUser, "User").Result;

                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"Successfully created test user {testUsername}.");
                    }
                    else
                    {
                        Console.WriteLine($"Error adding test user to role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Error creating test user {testUsername}: {string.Join(", ", testUserCreated.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    public static void CreateTestRoomSync(IApplicationBuilder app)
    {
        lock (LockObject)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

            if (roomService.GetRoomById(new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a9")).Result != null)
            {
                return;
            }

            roomService.RegisterRoomAsync("test", "test", new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"), "").Wait();
        }
    }
}