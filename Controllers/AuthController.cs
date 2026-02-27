using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio_Backend.Data;
using Portfolio_Backend.Helpers;
using Portfolio_Backend.Models;
using Portfolio_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Portfolio_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenHelper _jwtHelper;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService, IWebHostEnvironment env)
    {
        _context = context;
        _jwtHelper = new JwtTokenHelper(config);
        _emailService = emailService;
        _config = config;
        _env = env;
    }

[HttpPost("register")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
{
    try
    {
        var emailExists = await _context.Users
            .Where(u => u.Email == userDto.Email)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (emailExists != Guid.Empty)
            return BadRequest("User already exists.");

        var username = (userDto.Username ?? "").Trim();
        if (string.IsNullOrEmpty(username))
        {
            var atIndex = userDto.Email.IndexOf('@');
            username = atIndex > 0
                ? userDto.Email[..atIndex].Trim()
                : userDto.Email.Trim();
            if (string.IsNullOrEmpty(username))
                username = "user";
        }

        var usernameTaken = await _context.Users.AnyAsync(u => u.Username == username);
        if (usernameTaken)
        {
            var baseUsername = username;
            var suffix = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{suffix}";
                suffix++;
            }
        }

        var user = new AppUser
        {
            Email = userDto.Email,
            Username = username,
            PasswordHash = HashPassword(userDto.Password),
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine("Insert failed (possibly duplicate ID): " + dbEx.Message);
            return Conflict("User registration failed due to a duplicate entry.");
        }

        var jwt = _jwtHelper.GenerateToken(user);
        SetTokenCookie(jwt);

        return Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Register failed: " + ex.Message);
        return StatusCode(500, "Database error");
    }
}


[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login([FromBody] LoginRequest login)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
    if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
        return Unauthorized("Invalid credentials.");    

    var jwt = _jwtHelper.GenerateToken(user);
    SetTokenCookie(jwt);

    return Ok(new { isAdmin = user.IsAdmin, username = user.Username });
}

[Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me()
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
        return Unauthorized();
    var user = await _context.Users.FindAsync(userId);
    if (user == null)
        return Unauthorized();

    return Ok(new { userId = user.Id, email = user.Email, username = user.Username, isAdmin = user.IsAdmin, createdAt = user.CreatedAt });
}

[Authorize]
[HttpPut("me")]
public async Task<IActionResult> UpdateMyUsername([FromBody] UpdateUsernameDto dto)
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(sub, out Guid userId))
        return Unauthorized();

    var username = (dto?.Username ?? "").Trim();
    if (string.IsNullOrEmpty(username))
        return BadRequest("Username cannot be empty.");

    var takenByOther = await _context.Users
        .AnyAsync(u => u.Username == username && u.Id != userId);
    if (takenByOther)
        return BadRequest("Username is already taken.");

    var user = await _context.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Unauthorized();

    user.Username = username;
    await _context.SaveChangesAsync();

    return Ok(new { username = user.Username });
}

[HttpPost("forgot-password")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    // Always return 200 OK to avoid user enumeration
    if (string.IsNullOrWhiteSpace(request.Email))
        return Ok();

    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null)
        return Ok();

    var tokenBytes = new byte[32];
    RandomNumberGenerator.Fill(tokenBytes);
    var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    var resetToken = new PasswordResetToken
    {
        UserId = user.Id,
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    };
    _context.PasswordResetTokens.Add(resetToken);
    await _context.SaveChangesAsync();

    var frontendBaseUrl = _config["Frontend:BaseUrl"] ?? _config["FRONTEND_URL"] ?? "http://localhost:5173";
    await _emailService.SendPasswordResetEmailAsync(user.Email, token, frontendBaseUrl);

    return Ok();
}

[HttpPost("reset-password")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        return BadRequest("Token and new password are required.");

    var resetRecord = await _context.PasswordResetTokens
        .FirstOrDefaultAsync(t => t.Token == request.Token && t.ExpiresAt > DateTime.UtcNow);
    if (resetRecord == null)
        return BadRequest("Invalid or expired reset token.");

    var user = await _context.Users
        .AsTracking()
        .FirstOrDefaultAsync(u => u.Id == resetRecord.UserId);
    if (user == null)
        return BadRequest("User not found.");

    user.PasswordHash = HashPassword(request.NewPassword);
    _context.PasswordResetTokens.Remove(resetRecord);
    await _context.SaveChangesAsync();

    return Ok();
}

[Authorize]
[HttpGet("users")]
public async Task<IActionResult> GetUsers()
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out Guid currentUserId))
        return Unauthorized("Invalid or missing user token.");

    var currentUser = await _context.Users.FindAsync(currentUserId);
    if (currentUser == null || !currentUser.IsAdmin)
        return Forbid();

    try
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Username,
                u.IsAdmin,
                createdAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42703")
    {
        // Column "CreatedAt" does not exist yet - return users without it
        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Email, u.Username, u.IsAdmin, createdAt = (DateTime?)null })
            .ToListAsync();
        return Ok(users);
    }
}

[Authorize]
[HttpGet("users/{id:guid}")]
public async Task<IActionResult> GetUser(Guid id)
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out Guid currentUserId))
        return Unauthorized("Invalid or missing user token.");

    var currentUser = await _context.Users.FindAsync(currentUserId);
    if (currentUser == null || !currentUser.IsAdmin)
        return Forbid();

    var user = await _context.Users
        .Where(u => u.Id == id)
        .Select(u => new { u.Id, u.Email, u.Username, u.IsAdmin, u.CreatedAt })
        .FirstOrDefaultAsync();
    if (user == null)
        return NotFound("User not found.");

    return Ok(user);
}

[Authorize]
[HttpPut("users/{id:guid}")]
public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateDto dto)
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var currentUserId))
        return Unauthorized();
    var currentUser = await _context.Users.FindAsync(currentUserId);
    if (currentUser == null || !currentUser.IsAdmin)
        return Forbid();

    var user = await _context.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == id);
    if (user == null)
        return NotFound("User not found.");

    if (dto.Username != null)
    {
        var username = dto.Username.Trim();
        if (string.IsNullOrEmpty(username))
            return BadRequest("Username cannot be empty.");
        var takenByOther = await _context.Users.AnyAsync(u => u.Username == username && u.Id != id);
        if (takenByOther)
            return BadRequest("Username is already taken.");
        user.Username = username;
    }
    if (dto.Email != null)
    {
        var email = dto.Email.Trim();
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email cannot be empty.");
        var takenByOther = await _context.Users.AnyAsync(u => u.Email == email && u.Id != id);
        if (takenByOther)
            return BadRequest("Email is already in use.");
        user.Email = email;
    }
    if (dto.IsAdmin.HasValue)
        user.IsAdmin = dto.IsAdmin.Value;

    await _context.SaveChangesAsync();

    return Ok(new { user.Id, user.Email, user.Username, user.IsAdmin, user.CreatedAt });
}

[HttpPost("logout")]
[EnableRateLimiting("auth")]
public IActionResult Logout()
{
    Response.Cookies.Append("access_token", "", GetAccessTokenCookieOptions(expire: true));
    return NoContent();
}

private void SetTokenCookie(string token)
{
    Response.Cookies.Append("access_token", token, GetAccessTokenCookieOptions(expire: false));
}

private CookieOptions GetAccessTokenCookieOptions(bool expire)
{
    var isProduction = _env.IsProduction();
    return new CookieOptions
    {
        HttpOnly = true,
        Secure = isProduction,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expire ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddHours(2)
    };
}
    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string input, string hash)
    {
        return HashPassword(input) == hash;
    }
}
