namespace Portfolio_Backend.Models;

public class AdminUserUpdateDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool? IsAdmin { get; set; }
}
