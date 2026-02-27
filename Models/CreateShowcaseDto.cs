using System.Text.Json.Serialization;

namespace Portfolio_Backend.Models;

public class CreateShowcaseDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "site";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("userIds")]
    public List<string>? UserIds { get; set; }

    [JsonPropertyName("htmlContent")]
    public string? HtmlContent { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}
