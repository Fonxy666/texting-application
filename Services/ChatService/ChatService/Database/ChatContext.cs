using Microsoft.EntityFrameworkCore;
using ChatService.Model;

namespace ChatService.Database;

public class ChatContext : DbContext
{
    public DbSet<Room>? Rooms { get; set; }
    public DbSet<Message>? Messages { get; set; }

    public ChatContext(DbContextOptions<ChatContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Room)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}