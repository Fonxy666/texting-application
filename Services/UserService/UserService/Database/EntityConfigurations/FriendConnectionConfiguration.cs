using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Models;

namespace UserService.Database.EntityConfigurations;

public class FriendConnectionConfiguration : IEntityTypeConfiguration<FriendConnection>
{
    public void Configure(EntityTypeBuilder<FriendConnection> builder)
    {
        builder.HasKey(fc => fc.ConnectionId);

        builder.Property(fc => fc.SenderId).IsRequired();
        builder.Property(fc => fc.ReceiverId).IsRequired();

        builder.Property(fc => fc.Status)
               .IsRequired()
               .HasDefaultValue(FriendStatus.Pending);

        builder.Property(fc => fc.SentTime)
               .IsRequired()
               .HasDefaultValueSql("current_timestamp");

        builder.Property(fc => fc.AcceptedTime)
               .IsRequired(false);

        builder.HasOne(fc => fc.Sender)
               .WithMany(u => u.SentFriendRequests)
               .HasForeignKey(fc => fc.SenderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fc => fc.Receiver)
               .WithMany(u => u.ReceivedFriendRequests)
               .HasForeignKey(fc => fc.ReceiverId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}