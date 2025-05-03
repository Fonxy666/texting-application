using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Database.EntityConfigurations;
public class EncryptedSymmetricKeyConfiguration : IEntityTypeConfiguration<EncryptedSymmetricKey>
{
    public void Configure(EntityTypeBuilder<EncryptedSymmetricKey> builder)
    {
        builder.HasKey(k => k.KeyId);
        builder.Property(u => u.UserId).IsRequired();
        builder.Property(u => u.RoomId).IsRequired();

        builder.Property(k => k.EncryptedKey)
               .IsRequired()
               .HasMaxLength(1024);

        builder.HasOne(k => k.User)
               .WithMany(u => u.UserSymmetricKeys)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
