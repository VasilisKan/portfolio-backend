using Microsoft.EntityFrameworkCore;
using Portfolio_Backend.Models;

namespace Portfolio_Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<PortfolioTickets> Tickets { get; set; }
}

