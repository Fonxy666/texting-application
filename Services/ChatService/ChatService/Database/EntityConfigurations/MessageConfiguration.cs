using System.Text.Json;
using ChatService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Database.EntityConfigurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.SenderId)
            .IsRequired();
        
        builder.Property(m => m.Text)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(m => m.SendTime)
            .IsRequired();
        
        builder.Property(m => m.SentAsAnonymous)
            .IsRequired();
        
        builder.Property(m => m.Iv)
            .IsRequired();
        
        builder.Property(m => m.Seen)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null))
            .HasColumnType("jsonb");

        builder.HasOne(m => m.Room)
            .WithMany(m => m.Messages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(m => m.RoomId);
    }
}