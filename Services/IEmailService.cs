namespace Portfolio_Backend.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string frontendBaseUrl, CancellationToken cancellationToken = default);
}
