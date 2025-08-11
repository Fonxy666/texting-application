using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatService.Database;

public class ChatContextFactory : IDesignTimeDbContextFactory<ChatContext>
{
    public ChatContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("chat-service-test-config.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ChatContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? configuration["ChatTestDbConnectionString"];

        optionsBuilder.UseNpgsql(connectionString);

        return new ChatContext(optionsBuilder.Options);
    }
}