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
[Route("ticket/[controller]")]

public class TicketSubmitController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenHelper _jwtHelper;

    public TicketSubmitController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _jwtHelper = new JwtTokenHelper(config);
    }
    [HttpPost("submit")]
public async Task<IActionResult> SubmitTicket([FromBody] PortfolioTickets ticket)
{
    try
    {
        if (ticket == null || string.IsNullOrEmpty(ticket.Title) || string.IsNullOrEmpty(ticket.Description))
        {
            return BadRequest("Required fields cannot be null or empty");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID is invalid or missing from token.");
        }

        if (ticket.UserId == Guid.Empty)
        {
            ticket.UserId = userId;
        }

        var user = await _context.Users.FindAsync(ticket.UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ticket submitted successfully", ticketId = ticket.Id });
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}

    [HttpGet("get")]
    public async Task<IActionResult> GetTickets()
    {
        try
        {
            var tickets = await _context.Tickets.ToListAsync();
            return Ok(tickets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] PortfolioTickets updatedTicket)
    {
        try
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            ticket.Title = updatedTicket.Title;
            ticket.Description = updatedTicket.Description;
            ticket.Category = updatedTicket.Category;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
        {
            try
            {
                var ticket = await _context.Tickets.FindAsync(id);
                if (ticket == null)
                {
                    return NotFound("Ticket not found.");
                }

                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Ticket deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

}