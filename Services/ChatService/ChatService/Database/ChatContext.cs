using Microsoft.EntityFrameworkCore;
using ChatService.Model;

namespace ChatService.Database;

public class ChatContext(DbContextOptions<ChatContext> options) : DbContext(options)
{
    public DbSet<Room>? Rooms { get; set; }
    public DbSet<Message>? Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatContext).Assembly);
    }
}