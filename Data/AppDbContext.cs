using Microsoft.EntityFrameworkCore;
using Portfolio_Backend.Models;

namespace Portfolio_Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<PortfolioTickets> Tickets { get; set; }
    public DbSet<TicketReply> TicketReplies { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Demo> Demos { get; set; }
    public DbSet<DemoAssignment> DemoAssignments { get; set; }
    public DbSet<Showcase> Showcase { get; set; }
    public DbSet<ShowcaseAssignment> ShowcaseAssignments { get; set; }
}

