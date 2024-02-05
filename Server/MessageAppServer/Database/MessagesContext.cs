using Microsoft.EntityFrameworkCore;
using Server.Model.Chat;

namespace Server.Database;

public class MessagesContext : DbContext
{
    public DbSet<Message> Messages { get; set; }
    
    public MessagesContext(DbContextOptions<MessagesContext> options) : base(options) { }
}