using Microsoft.EntityFrameworkCore;
using TicketDesk.Models;

namespace TicketDesk.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
}