namespace Portfolio_Backend.Models;

public class DemoAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DemoId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Demo? Demo { get; set; }
    public AppUser? User { get; set; }
}
