using Microsoft.EntityFrameworkCore;
using AuthenticationServer.Model;

namespace AuthenticationServer.Database;

public class PrivateKeysDbContext : DbContext
{
    public DbSet<PrivateKey>? Keys { get; set; }
    
    public PrivateKeysDbContext(DbContextOptions<PrivateKeysDbContext> options) : base(options) { }
}