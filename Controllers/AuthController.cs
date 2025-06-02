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
        _context = context;
        _jwtHelper = new JwtTokenHelper(config);
    }

[HttpPost("register")]
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

        var user = new AppUser
        {
            Email = userDto.Email,
            PasswordHash = HashPassword(userDto.Password),
            IsAdmin = false  
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
public async Task<IActionResult> Login([FromBody] LoginRequest login)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
    if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
        return Unauthorized("Invalid credentials.");    

    var jwt = _jwtHelper.GenerateToken(user);
    SetTokenCookie(jwt);

    return Ok(new { /*userId = user.Id, */isAdmin = user.IsAdmin });
}

[Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me()
{
    var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var user = await _context.Users.FindAsync(Guid.Parse(sub));
    if (user == null)
        return Unauthorized();

    return Ok(new { /*userId = user.Id,*/ isAdmin = user.IsAdmin });
}

[HttpPost("logout")]
public IActionResult Logout()
{
    var cookieOptions = new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddDays(-1), 
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/"
    };
    Response.Cookies.Append("access_token", "", cookieOptions);
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
