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

        modelBuilder.Entity<Room>()
            .HasOne(r => r.CreatorUser)
            .WithMany(au => au.CreatedRooms)
            .HasForeignKey(r => r.CreatorId);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Room)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<EncryptedSymmetricKey>()
            .HasOne<Room>(k => k.Room)
            .WithMany(r => r.EncryptedSymmetricKeys)
            .HasForeignKey(k => k.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<EncryptedSymmetricKey>()
            .HasOne<ApplicationUser>(k => k.User)
            .WithMany(u => u.UsersSymmetricKeys)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}