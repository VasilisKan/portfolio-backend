namespace Portfolio_Backend.Models;

public class Showcase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "site"; // "site" | "photo"
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public AppUser? CreatedByUser { get; set; }
    public ICollection<ShowcaseAssignment> Assignments { get; set; } = new List<ShowcaseAssignment>();
}
