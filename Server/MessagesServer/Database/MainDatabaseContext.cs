using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MessagesServer.Model;

namespace MessagesServer.Database;

public class MainDatabaseContext : DbContext
{
    public DbSet<Room>? Rooms { get; set; }
    public DbSet<Message>? Messages { get; set; }

    public MainDatabaseContext(DbContextOptions<MainDatabaseContext> options)
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