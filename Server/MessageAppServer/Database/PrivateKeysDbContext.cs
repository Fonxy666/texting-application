using Microsoft.EntityFrameworkCore;
using Server.Model;

namespace Server.Database;

public class PrivateKeysDbContext : DbContext
{
    public DbSet<PrivateKey>? Keys { get; set; }
    
    public PrivateKeysDbContext(DbContextOptions<PrivateKeysDbContext> options) : base(options) { }
}