using Microsoft.EntityFrameworkCore;
using Server.Model.Chat;

namespace Server.Database;

public class MessagesContext(DbContextOptions<MessagesContext> options, DbSet<Message> messages)
    : DbContext(options)
{
    public DbSet<Message> Messages { get; set; } = messages;
}