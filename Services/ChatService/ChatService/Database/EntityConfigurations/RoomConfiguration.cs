using ChatService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatService.Database.EntityConfigurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.RoomId);
        
        builder.Property(r => r.RoomId)
            .IsRequired();
        
        builder.Property(r => r.CreatorId)
            .IsRequired();
        
        builder.Property(r => r.RoomName)
            .IsRequired();
        
        builder.Property(r => r.Password)
            .IsRequired()
            .HasMaxLength(1024);
        
        
    }
}