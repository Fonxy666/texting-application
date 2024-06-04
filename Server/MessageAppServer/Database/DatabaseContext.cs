using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Model;
using Server.Model.Chat;

namespace Server.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Room>? Rooms { get; set; }
    public DbSet<Message>? Messages { get; set; }
    public DbSet<FriendConnection>? FriendConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FriendConnection>()
            .HasOne(fc => fc.Sender)
            .WithMany(au => au.SentFriendRequests)
            .HasForeignKey(fc => fc.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FriendConnection>()
            .HasOne(fc => fc.Receiver)
            .WithMany(au => au.ReceivedFriendRequests)
            .HasForeignKey(fc => fc.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<ApplicationUser>()
            .HasMany(au => au.Friends)
            .WithMany()
            .UsingEntity(j => j.ToTable("UserFriends"));
    }
}