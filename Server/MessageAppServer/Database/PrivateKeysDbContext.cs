using Microsoft.EntityFrameworkCore;
using Server.Model;

namespace Server.Database;

public class PrivateKeysDbContext(DbContextOptions<PrivateKeysDbContext> options) : DbContext(options)
{
    public DbSet<PrivateKey>? Keys { get; set; }
}