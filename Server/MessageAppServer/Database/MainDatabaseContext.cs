using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthenticationServer.Model;
using AuthenticationServer.Model.Chat;

namespace AuthenticationServer.Database;

public class MainDatabaseContext(DbContextOptions<MainDatabaseContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<FriendConnection>? FriendConnections { get; set; }
    public DbSet<EncryptedSymmetricKey>? EncryptedSymmetricKeys { get; set; }

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
        
        modelBuilder.Entity<ApplicationUser>()
            .HasMany(au => au.CreatedRooms)
            .WithOne(r => r.CreatorUser)
            .HasForeignKey(r => r.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<EncryptedSymmetricKey>()
            .HasOne<ApplicationUser>(k => k.User)
            .WithMany(u => u.UserSymmetricKeys)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}