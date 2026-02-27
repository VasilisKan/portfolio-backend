using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio_Backend.Data;
using Portfolio_Backend.Models;

namespace Portfolio_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShowcaseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public ShowcaseController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var (currentUserId, isAdmin) = await GetCurrentUserInfo();
        if (currentUserId == null)
            return Unauthorized();

        IQueryable<Guid> idQuery = _context.Showcase.Select(s => s.Id);
        if (!isAdmin)
        {
            var assignedIds = await _context.ShowcaseAssignments
                .Where(a => a.UserId == currentUserId.Value)
                .Select(a => a.ShowcaseId)
                .ToListAsync();
            idQuery = _context.Showcase.Where(s => assignedIds.Contains(s.Id)).Select(s => s.Id);
        }

        var showcaseIds = await idQuery.ToListAsync();
        var items = await _context.Showcase
            .AsNoTracking()
            .Where(s => showcaseIds.Contains(s.Id))
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.Id, s.Type, s.Title, s.Slug, s.HtmlContent, s.ImageUrl, s.CreatedAt, s.UpdatedAt })
            .ToListAsync();

        var assignmentsByShowcase = await _context.ShowcaseAssignments
            .AsNoTracking()
            .Where(a => showcaseIds.Contains(a.ShowcaseId))
            .Select(a => new { a.ShowcaseId, a.UserId })
            .ToListAsync();

        var lookup = assignmentsByShowcase
            .GroupBy(a => a.ShowcaseId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToList());

        var result = items.Select(s => new
        {
            id = s.Id,
            type = s.Type,
            title = s.Title,
            slug = s.Slug,
            htmlContent = s.HtmlContent,
            imageUrl = s.ImageUrl,
            userIds = lookup.GetValueOrDefault(s.Id) ?? new List<Guid>(),
            createdAt = s.CreatedAt,
            updatedAt = s.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShowcaseDto dto)
    {
        var (currentUserId, isAdmin) = await GetCurrentUserInfo();
        if (currentUserId == null)
            return Unauthorized();
        if (!isAdmin)
            return Forbid();

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var type = (dto.Type ?? "").Trim().ToLowerInvariant();
        if (type != "site" && type != "photo")
            (errors.TryAdd("type", new List<string>()) ? errors["type"] : errors["type"]).Add("type must be 'site' or 'photo' (lowercase).");

        if (string.IsNullOrWhiteSpace(dto.Title))
            (errors.TryAdd("title", new List<string>()) ? errors["title"] : errors["title"]).Add("title is required and must be non-empty.");
        var title = (dto.Title ?? "").Trim();

        if (dto.UserIds == null || dto.UserIds.Count == 0)
            (errors.TryAdd("userIds", new List<string>()) ? errors["userIds"] : errors["userIds"]).Add("userIds is required (array of user UUIDs).");

        var userIdsParsed = new List<Guid>();
        if (dto.UserIds != null)
        {
            foreach (var s in dto.UserIds)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                if (!Guid.TryParse(s.Trim(), out var uid))
                {
                    (errors.TryAdd("userIds", new List<string>()) ? errors["userIds"] : errors["userIds"]).Add($"Invalid UUID: {s}");
                    break;
                }
                userIdsParsed.Add(uid);
            }
            if (dto.UserIds.Count > 0 && userIdsParsed.Count == 0 && !errors.ContainsKey("userIds"))
                (errors.TryAdd("userIds", new List<string>()) ? errors["userIds"] : errors["userIds"]).Add("userIds must contain at least one valid UUID.");
        }

        if (type == "site")
        {
            if (dto.HtmlContent == null)
                (errors.TryAdd("htmlContent", new List<string>()) ? errors["htmlContent"] : errors["htmlContent"]).Add("htmlContent is required when type is 'site' (can be empty string).");
        }
        else if (type == "photo")
        {
            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                (errors.TryAdd("imageUrl", new List<string>()) ? errors["imageUrl"] : errors["imageUrl"]).Add("imageUrl is required when type is 'photo'.");
            else if ((dto.ImageUrl ?? "").Length > 2048)
                (errors.TryAdd("imageUrl", new List<string>()) ? errors["imageUrl"] : errors["imageUrl"]).Add("imageUrl must be at most 2048 characters.");
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var slug = !string.IsNullOrWhiteSpace(dto.Slug)
            ? dto.Slug.Trim().ToLowerInvariant()
            : SlugFromTitle(title);

        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = "showcase";

        var suffix = 0;
        var baseSlug = slug;
        while (await _context.Showcase.AnyAsync(s => s.Slug == slug))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        var item = new Showcase
        {
            Type = type,
            Title = title,
            Slug = slug,
            HtmlContent = type == "site" ? (dto.HtmlContent ?? "") : null,
            ImageUrl = type == "photo" ? (dto.ImageUrl ?? "").Trim() : null,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Showcase.Add(item);
        await _context.SaveChangesAsync();

        foreach (var uid in userIdsParsed.Distinct())
        {
            _context.ShowcaseAssignments.Add(new ShowcaseAssignment
            {
                ShowcaseId = item.Id,
                UserId = uid,
                AssignedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var assignedIds = await _context.ShowcaseAssignments
            .Where(a => a.ShowcaseId == item.Id)
            .Select(a => a.UserId)
            .ToListAsync();

        var body = new
        {
            id = item.Id,
            type = item.Type,
            title = item.Title,
            slug = item.Slug,
            htmlContent = item.HtmlContent,
            imageUrl = item.ImageUrl,
            userIds = assignedIds,
            createdAt = item.CreatedAt,
            updatedAt = item.UpdatedAt
        };

        return CreatedAtAction(nameof(GetBySlug), new { slug = item.Slug }, body);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var (currentUserId, isAdmin) = await GetCurrentUserInfo();
        if (currentUserId == null)
            return Unauthorized();

        var item = await _context.Showcase
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug);
        if (item == null)
            return NotFound();

        if (!isAdmin)
        {
            var isAssigned = await _context.ShowcaseAssignments
                .AnyAsync(a => a.ShowcaseId == item.Id && a.UserId == currentUserId.Value);
            if (!isAssigned)
                return Forbid();
        }

        var userIds = await _context.ShowcaseAssignments
            .Where(a => a.ShowcaseId == item.Id)
            .Select(a => a.UserId)
            .ToListAsync();

        return Ok(new
        {
            id = item.Id,
            type = item.Type,
            title = item.Title,
            slug = item.Slug,
            htmlContent = item.HtmlContent,
            imageUrl = item.ImageUrl,
            userIds,
            createdAt = item.CreatedAt,
            updatedAt = item.UpdatedAt
        });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<IActionResult> Upload([FromForm(Name = "file")] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided or file is empty." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "File too large. Maximum size is 5 MB." });

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!AllowedContentTypes.Contains(contentType) && !contentType.StartsWith("image/"))
            return BadRequest(new { message = "Invalid file type. Only images are allowed (e.g. JPEG, PNG, GIF, WebP)." });

        var extension = GetExtensionFromContentType(contentType) ?? Path.GetExtension(file.FileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension))
            extension = "jpg";

        var fileName = $"{Guid.NewGuid():N}.{extension}";
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath ?? ".", "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, fileName);

        try
        {
            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }
        }
        catch (IOException ex)
        {
            return StatusCode(500, new { message = "Failed to save file.", detail = ex.Message });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/uploads/{fileName}";

        return Ok(new { url });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (currentUserId, isAdmin) = await GetCurrentUserInfo();
        if (currentUserId == null)
            return Unauthorized();
        if (!isAdmin)
            return Forbid();

        var item = await _context.Showcase.FindAsync(id);
        if (item == null)
            return NotFound();

        _context.Showcase.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<(Guid? UserId, bool IsAdmin)> GetCurrentUserInfo()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out Guid userId))
            return (null, false);

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.IsAdmin })
            .FirstOrDefaultAsync();

        return user == null ? (null, false) : (user.Id, user.IsAdmin);
    }

    private static string SlugFromTitle(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s\-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "showcase" : slug;
    }

    private static string? GetExtensionFromContentType(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            _ => null
        };
    }
}
