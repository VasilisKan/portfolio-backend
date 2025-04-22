using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio_Backend.Data;
using Portfolio_Backend.Helpers;
using Portfolio_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Portfolio_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenHelper _jwtHelper;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context   = context;
        _jwtHelper = new JwtTokenHelper(config);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AppUser user)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            return BadRequest("User already exists.");

        user.PasswordHash = HashPassword(user.PasswordHash);
        user.Id           = Guid.NewGuid();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var jwt = _jwtHelper.GenerateToken(user);
        SetTokenCookie(jwt);

        return Ok(new { userId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AppUser login)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
        if (user == null || !VerifyPassword(login.PasswordHash, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var jwt = _jwtHelper.GenerateToken(user);
        SetTokenCookie(jwt);

        return Ok(new { userId = user.Id });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new { userId = sub });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    private void SetTokenCookie(string token)
{
    var cookieOpts = new CookieOptions
    {
        HttpOnly = true,
        Secure   = true,               
        SameSite = SameSiteMode.None,
        Expires  = DateTimeOffset.UtcNow.AddHours(2)
    };
    Response.Cookies.Append("access_token", token, cookieOpts);
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
