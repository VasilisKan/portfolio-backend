namespace Portfolio_Backend.Services;

public class EmailOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseImplicitSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FromEmail);
}
