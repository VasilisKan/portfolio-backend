using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Portfolio_Backend.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(Microsoft.Extensions.Options.IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string frontendBaseUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            Console.WriteLine("[Email] SMTP not configured. Password reset email not sent.");
            return false;
        }

        var resetLink = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        var htmlBody = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><title>Reset your password</title></head>
            <body style="font-family: sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2>Reset your password</h2>
                <p>Hello,</p>
                <p>You requested a password reset for your account. Click the link below to set a new password:</p>
                <p><a href="{resetLink}" style="display: inline-block; padding: 12px 24px; background: #2563eb; color: white; text-decoration: none; border-radius: 6px;">Reset Password</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p style="word-break: break-all; color: #666;">{resetLink}</p>
                <p><strong>This link expires in 1 hour.</strong></p>
                <p>If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
                <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;">
                <p style="font-size: 12px; color: #888;">This is an automated message. Please do not reply.</p>
            </body>
            </html>
            """;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName ?? "Portfolio", _options.FromEmail!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Reset your password";

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

            var secureSocketOptions = _options.UseImplicitSsl || _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host!, _options.Port, secureSocketOptions, timeoutCts.Token);
            await client.AuthenticateAsync(_options.Username!, _options.Password!, timeoutCts.Token);
            await client.SendAsync(message, timeoutCts.Token);
            await client.DisconnectAsync(true, timeoutCts.Token);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email] Failed to send password reset: {ex.Message}");
            return false;
        }
    }
}
