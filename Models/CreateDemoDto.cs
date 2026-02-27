namespace Portfolio_Backend.Models;

public class CreateDemoDto
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public List<Guid>? UserIds { get; set; }
}
