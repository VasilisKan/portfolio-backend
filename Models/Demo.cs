namespace Portfolio_Backend.Models;

public class Demo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public AppUser? CreatedByUser { get; set; }
    public ICollection<DemoAssignment> Assignments { get; set; } = new List<DemoAssignment>();
}
