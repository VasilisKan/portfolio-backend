using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio_Backend.Data;
using Portfolio_Backend.Helpers;
using Portfolio_Backend.Models;

namespace Portfolio_Backend.Controllers
{
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

        [Authorize]
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTicket([FromBody] PortfolioTicketsDto ticketDto)
        {
            if (ticketDto == null || string.IsNullOrEmpty(ticketDto.Title) || string.IsNullOrEmpty(ticketDto.Description))
            {
                return BadRequest("Required fields cannot be null or empty");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("User ID is invalid or missing from token.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var ticket = new PortfolioTickets
            {
                Id = Guid.NewGuid(),
                Title = ticketDto.Title,
                Description = ticketDto.Description,
                Category = ticketDto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsResolved = false,
                UserId = userId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket submitted successfully", ticketId = ticket.Id });
        }

       [Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> GetTickets()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("User ID is invalid or missing from token.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!user.IsAdmin)
            {
                return Forbid("You are not authorized to view tickets.");
            }

            var tickets = await _context.Tickets
                .Join(_context.Users,
                    ticket => ticket.UserId,
                    user => user.Id,
                    (ticket, user) => new
                    {
                        ticket.Id,
                        ticket.Title,
                        ticket.Description,
                        ticket.Category,
                        ticket.CreatedAt,
                        ticket.UpdatedAt,
                        ticket.IsResolved,
                        UserEmail = user.Email  
                    })
                .ToListAsync();

            return Ok(tickets);
        }

        [Authorize]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] PortfolioTicketsDto ticketDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("User ID is invalid or missing from token.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!user.IsAdmin)
            {
                return Forbid("You are not authorized to update tickets.");
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            ticket.Title = ticketDto.Title;
            ticket.Description = ticketDto.Description;
            ticket.Category = ticketDto.Category;
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket updated successfully" });
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTicket(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("User ID is invalid or missing from token.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!user.IsAdmin)
            {
                return Forbid("You are not authorized to delete tickets.");
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket deleted successfully" });
        }
    }
}
