namespace Portfolio_Backend.Services;

public class CloudflareOptions
{
    public const string SectionName = "Cloudflare";

    public string? ApiToken { get; set; }
    public string? ZoneId { get; set; }
}
