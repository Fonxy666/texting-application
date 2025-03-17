using Microsoft.EntityFrameworkCore;
using AuthenticationService.Model;

namespace AuthenticationService.Database;

public class PrivateKeysDbContext : DbContext
{
    public DbSet<PrivateKey>? Keys { get; set; }
    
    public PrivateKeysDbContext(DbContextOptions<PrivateKeysDbContext> options) : base(options) { }
}