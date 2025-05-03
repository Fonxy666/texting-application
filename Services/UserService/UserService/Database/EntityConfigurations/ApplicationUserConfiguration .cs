using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Database.EntityConfigurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.PublicKey).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(512);

        builder.HasMany(u => u.ReceivedFriendRequests)
               .WithOne(fc => fc.Receiver)
               .HasForeignKey(fc => fc.ReceiverId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.SentFriendRequests)
               .WithOne(fc => fc.Sender)
               .HasForeignKey(fc => fc.SenderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.UserSymmetricKeys)
               .WithOne(k => k.User)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(u => u.Friends)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UserFriends",
                j => j
                    .HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey("FriendId")
                    .OnDelete(DeleteBehavior.Restrict),
                j => j
                    .HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "FriendId");
                    j.ToTable("UserFriends");
                }
            );
    }
}
