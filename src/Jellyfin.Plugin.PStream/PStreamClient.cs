using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PStream;

public class PStreamClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PStreamClient> _logger;

    public PStreamClient(HttpClient httpClient, ILogger<PStreamClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void Configure(string baseUrl, string apiKey)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
    }

    public async Task<IReadOnlyList<PStreamItem>> SearchAsync(string query, int? limit, CancellationToken cancellationToken)
    {
        var url = $"api/search?query={Uri.EscapeDataString(query)}";
        if (limit.HasValue)
        {
            url += $"&limit={limit.Value}";
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<PStreamSearchResponse>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return payload?.Results ?? Array.Empty<PStreamItem>();
    }

    public async Task<PStreamItem?> GetItemAsync(string id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/items/{Uri.EscapeDataString(id)}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<PStreamItem>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

public record PStreamSearchResponse
{
    public List<PStreamItem> Results { get; init; } = new();
}

public record PStreamItem
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Poster { get; init; }

    public string? StreamUrl { get; init; }

    public TimeSpan? Duration { get; init; }
}
