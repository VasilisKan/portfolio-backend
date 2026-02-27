using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio_Backend.Services;

namespace Portfolio_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CloudflareController : ControllerBase
{
    private readonly ICloudflareApiService _cloudflare;

    public CloudflareController(ICloudflareApiService cloudflare)
    {
        _cloudflare = cloudflare;
    }

    [HttpGet("analytics/dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? since = null,
        [FromQuery] DateTime? until = null,
        [FromQuery] bool continuous = false,
        CancellationToken cancellationToken = default)
    {
        var (success, json, error) = await _cloudflare.GetDashboardAsync(since, until, continuous, cancellationToken).ConfigureAwait(false);

        if (!success)
        {
            return BadRequest(new { error });
        }

        // Return raw JSON so frontend gets full Cloudflare response (result.timeseries, result.totals, etc.)
        return Content(json!, "application/json");
    }
}
