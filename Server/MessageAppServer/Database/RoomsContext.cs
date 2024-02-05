using Microsoft.EntityFrameworkCore;
using Server.Model.Chat;

namespace Server.Database;

public class RoomsContext : DbContext
{
    public DbSet<Room> Rooms { get; set; }
    public RoomsContext(DbContextOptions<RoomsContext> options) : base(options) { }
}