namespace Portfolio_Backend.Models
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();  
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
    }
}
