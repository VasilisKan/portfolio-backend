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
        [HttpPost("{ticketId:guid}/reply")]
        public async Task<IActionResult> ReplyToTicket(Guid ticketId, [FromBody] TicketReplyDto replyDto)
        {
            if (replyDto == null || string.IsNullOrWhiteSpace(replyDto.Message))
                return BadRequest("Message is required.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return Unauthorized("User ID is invalid or missing from token.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return NotFound("Ticket not found.");

            if (ticket.UserId != userId && !user.IsAdmin)
                return Forbid("You can only reply to your own tickets, or you must be an admin.");

            var reply = new TicketReply
            {
                TicketId = ticketId,
                UserId = userId,
                Message = replyDto.Message.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.TicketReplies.Add(reply);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reply added successfully", replyId = reply.Id });
        }

        [Authorize]
        [HttpGet("{ticketId:guid}/replies")]
        public async Task<IActionResult> GetTicketReplies(Guid ticketId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return Unauthorized("User ID is invalid or missing from token.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return NotFound("Ticket not found.");

            if (ticket.UserId != userId && !user.IsAdmin)
                return Forbid("You can only view replies for your own tickets, or you must be an admin.");

            var replies = await _context.TicketReplies
                .Where(r => r.TicketId == ticketId)
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new
                    {
                        r.Id,
                        r.TicketId,
                        r.UserId,
                        UserEmail = u.Email,
                        r.Message,
                        r.CreatedAt
                    })
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            return Ok(replies);
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

            var query = _context.Tickets
                .Join(_context.Users,
                    ticket => ticket.UserId,
                    u => u.Id,
                    (ticket, u) => new
                    {
                        ticket.Id,
                        ticket.Title,
                        ticket.Description,
                        ticket.Category,
                        ticket.CreatedAt,
                        ticket.UpdatedAt,
                        ticket.IsResolved,
                        ticket.UserId,
                        UserEmail = u.Email
                    });

            if (!user.IsAdmin)
            {
                query = query.Where(x => x.UserId == userId);
            }

            var tickets = await query
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.Category,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.IsResolved,
                    x.UserEmail
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [Authorize]
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetTicket(Guid id)
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

            var ticket = await _context.Tickets
                .Where(t => t.Id == id)
                .Join(_context.Users,
                    t => t.UserId,
                    u => u.Id,
                    (t, u) => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Category,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.IsResolved,
                        t.UserId,
                        UserEmail = u.Email
                    })
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            if (ticket.UserId != userId && !user.IsAdmin)
            {
                return Forbid("You can only view your own tickets.");
            }

            return Ok(new
            {
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.Category,
                ticket.CreatedAt,
                ticket.UpdatedAt,
                ticket.IsResolved,
                ticket.UserEmail
            });
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

            var replies = await _context.TicketReplies.Where(r => r.TicketId == id).ToListAsync();
            _context.TicketReplies.RemoveRange(replies);
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket deleted successfully" });
        }

        [Authorize]
        [HttpPut("resolve/{id}")]
        public async Task<IActionResult> ResolveTicket(Guid id)
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
                return Forbid("You are not authorized to resolve tickets.");
            }

            var ticket = await _context.Tickets
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            ticket.IsResolved = true;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket resolved successfully" });
        }

        [Authorize]
        [HttpPut("reopen/{id}")]
        public async Task<IActionResult> ReopenTicket(Guid id)
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
                return Forbid("You are not authorized to reopen tickets.");
            }

            var ticket = await _context.Tickets
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            ticket.IsResolved = false;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ticket reopened successfully" });
        }
    }
}
