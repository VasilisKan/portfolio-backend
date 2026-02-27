using System.Text;
using System.Text.Json;

namespace Portfolio_Backend.Services;

public class CloudflareApiService : ICloudflareApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CloudflareOptions _options;

    private const string GraphqlEndpoint = "https://api.cloudflare.com/client/v4/graphql";

    // Uses httpRequestsAdaptiveGroups (available on all plans including Free). httpRequests1mGroups requires paid plans.
    private const string ZoneAnalyticsQuery = "query ZoneAnalytics($zoneTag: string, $start: Time, $end: Time) { viewer { zones(filter: { zoneTag: $zoneTag }) { httpRequestsAdaptiveGroups(limit: 1000, orderBy: [datetimeHour_ASC], filter: { datetime_geq: $start, datetime_lt: $end }) { count dimensions { datetimeHour } sum { visits edgeResponseBytes } } } } }";

    public CloudflareApiService(IHttpClientFactory httpClientFactory, Microsoft.Extensions.Options.IOptions<CloudflareOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<(bool Success, string? Json, string? Error)> GetDashboardAsync(
        DateTime? since = null,
        DateTime? until = null,
        bool continuous = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
            return (false, null, "Cloudflare ApiToken is not configured. Set Cloudflare__ApiToken in .env");
        if (string.IsNullOrWhiteSpace(_options.ZoneId))
            return (false, null, "Cloudflare ZoneId is not configured. Set Cloudflare__ZoneId in .env");

        var untilUtc = until?.Kind == DateTimeKind.Utc ? until.Value : (until ?? DateTime.UtcNow).ToUniversalTime();
        var sinceUtc = since?.Kind == DateTimeKind.Utc ? since.Value : (since ?? untilUtc.AddDays(-1)).ToUniversalTime();

        var sinceStr = sinceUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var untilStr = untilUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var payload = new
        {
            query = ZoneAnalyticsQuery,
            variables = new
            {
                zoneTag = _options.ZoneId.Trim(),
                start = sinceStr,
                end = untilStr
            }
        };
        var jsonPayload = JsonSerializer.Serialize(payload);

        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphqlEndpoint);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _options.ApiToken.Trim());
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return (false, null, "Cloudflare request failed: " + ex.Message);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var err = TryParseCloudflareError(body) ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            return (false, null, err);
        }

        // GraphQL returns 200 even when there are errors; check response.errors
        var graphqlErr = TryParseGraphQLErrors(body);
        if (graphqlErr != null)
            return (false, null, graphqlErr);

        return (true, body, null);
    }

    private static string? TryParseCloudflareError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var msg = first.TryGetProperty("message", out var m) ? m.GetString() : null;
                var code = first.TryGetProperty("code", out var c) ? c.GetInt32() : (int?)null;
                return code.HasValue ? $"[{code}] {msg}" : msg;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static string? TryParseGraphQLErrors(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var msg = first.TryGetProperty("message", out var m) ? m.GetString() : "GraphQL error";
                return msg;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
