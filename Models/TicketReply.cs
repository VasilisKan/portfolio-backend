namespace Portfolio_Backend.Models;

public class TicketReply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PortfolioTickets? Ticket { get; set; }
    public AppUser? User { get; set; }
}
