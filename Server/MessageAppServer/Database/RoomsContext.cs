using Microsoft.EntityFrameworkCore;
using Server.Model.Chat;

namespace Server.Database;

public class RoomsContext(DbContextOptions<RoomsContext> options, DbSet<Room> rooms) : DbContext(options)
{
    public DbSet<Room> Rooms { get; set; } = rooms;
}