using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MessagesServer.Model;
using MessagesServer.Model.Chat;

namespace MessagesServer.Database;

public class MainDatabaseContext(DbContextOptions<MainDatabaseContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Room>? Rooms { get; set; }
    public DbSet<Message>? Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.CreatorUser)
            .WithMany(au => au.CreatedRooms)
            .HasForeignKey(r => r.CreatorId);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Room)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}