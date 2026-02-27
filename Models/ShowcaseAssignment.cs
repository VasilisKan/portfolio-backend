namespace Portfolio_Backend.Models;

public class ShowcaseAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShowcaseId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Showcase? Showcase { get; set; }
    public AppUser? User { get; set; }
}
