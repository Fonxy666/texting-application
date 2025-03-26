using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserService.Model;

namespace UserService.Database;

public class MainDatabaseContext(DbContextOptions<MainDatabaseContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<FriendConnection>? FriendConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FriendConnection>()
            .HasOne(fc => fc.Sender)
            .WithMany(au => au.SentFriendRequests)
            .HasForeignKey(fc => fc.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

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