namespace Portfolio_Backend.Services;

public interface ICloudflareApiService
{
    Task<(bool Success, string? Json, string? Error)> GetDashboardAsync(
        DateTime? since = null,
        DateTime? until = null,
        bool continuous = false,
        CancellationToken cancellationToken = default);
}
