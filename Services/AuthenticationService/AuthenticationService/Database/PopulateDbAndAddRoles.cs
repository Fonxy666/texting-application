using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using AuthenticationService.Model;
using AuthenticationService.Services.PrivateKey;

namespace AuthenticationService.Database;

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

            //CreateAdminIfNotExistSync(userManager, configuration);
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
                    Email = adminEmail,
                    PublicKey = "PublicTestKey"
                };

                admin.SetPublicKey("");
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
}

    /* public static void CreateTestUsersSync(IApplicationBuilder app, int numberOfTestUsers)
    {
        lock (LockObject)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var keyManager = scope.ServiceProvider.GetRequiredService<IPrivateKeyService>();

            for (var i = 1; i <= numberOfTestUsers; i++)
            {
                var testEmail = $"test{i}@hotmail.com";
                var testUsername = $"TestUsername{i}";

                var keysForTestUser = GenerateAsymmetricKeys();

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
                    TwoFactorEnabled = i != 3,
                    PublicKey = keysForTestUser.PublicKey
                };

                var encryptedData = EncryptPrivateKey(keysForTestUser.PrivateKey, "123456");

                keyManager.SaveKey(new PrivateKey(encryptedData.EncryptedData.ToString()!, encryptedData.Iv.ToString()!), testUser.Id);

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

    private static AsymmetricKey GenerateAsymmetricKeys()
    {
        using var rsa = new RSACryptoServiceProvider(2048);
        try
        {
            var publicKey = rsa.ToXmlString(false); // false to get the public key only
            var privateKey = rsa.ToXmlString(true); // true to get both the public and private keys

            return new AsymmetricKey(publicKey, privateKey);
        }
        finally
        {
            rsa.PersistKeyInCsp = false;
        }
    }
    
    private const int KeySize = 256 / 8;
    private const int IvSize = 12;
    private const int TagSize = 16;
    private const int Iterations = 10000;

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }

    private static (byte[] Iv, byte[] EncryptedData) EncryptPrivateKey(string privateKey, string password)
    {
        var salt = GenerateRandomBytes(KeySize);
        var key = DeriveKey(password, salt);
        var iv = GenerateRandomBytes(IvSize);

        using var aesGcm = new AesGcm(key);
        var plaintext = Encoding.UTF8.GetBytes(privateKey);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        aesGcm.Encrypt(iv, plaintext, ciphertext, tag);

        var encryptedData = new byte[salt.Length + iv.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(salt, 0, encryptedData, 0, salt.Length);
        Buffer.BlockCopy(iv, 0, encryptedData, salt.Length, iv.Length);
        Buffer.BlockCopy(tag, 0, encryptedData, salt.Length + iv.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, salt.Length + iv.Length + tag.Length, ciphertext.Length);

        return (iv, encryptedData);
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }
}

public record AsymmetricKey(string PublicKey, string PrivateKey); */