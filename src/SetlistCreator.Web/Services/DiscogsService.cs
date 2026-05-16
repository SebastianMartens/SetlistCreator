using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SetlistCreator.Backend.Models;

namespace SetlistCreator.Web.Services;

public sealed class DiscogsService : IMusicSearchService
{
    private readonly HttpClient _http;

    public DiscogsService(HttpClient http) => _http = http;

    public bool IsConfigured => _http.DefaultRequestHeaders.Contains("Authorization");

    public async Task<IReadOnlyList<ArtistResult>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<SearchResponse>(
            $"database/search?q={Uri.EscapeDataString(query)}&type=artist&per_page=10", cancellationToken);

        return response?.Results
            .Select(r => new ArtistResult(r.Id.ToString(), r.Title, null))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AlbumResult>> GetArtistAlbumsAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<ArtistReleasesResponse>(
            $"artists/{artistId}/releases?per_page=100&sort=year&sort_order=asc", cancellationToken);

        return response?.Releases
            //.Where(r => r.Type == "master" && r.Role == "Main")
            .Select(r => new AlbumResult(r.Id.ToString(), r.Title, r.Year?.ToString()))
            .ToList() ?? [];
    }

    public async Task<Album> GetAlbumWithTracksAsync(string masterId, string albumTitle, CancellationToken cancellationToken = default)
    {
        var master = await _http.GetFromJsonAsync<MasterResponse>(
            $"masters/{masterId}", cancellationToken);

        var songs = master?.Tracklist
            .Where(t => t.Type_ != "heading" && !string.IsNullOrWhiteSpace(t.Title))
            .Select(t => new Song(Guid.NewGuid(), t.Title, ParseDuration(t.Duration), albumTitle))
            .ToList() ?? [];

        return new Album(DiscogsGuid(int.Parse(masterId)), albumTitle, songs);
    }

    private static TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return TimeSpan.FromMinutes(3);
        var parts = duration.Split(':');
        return parts.Length == 2
            && int.TryParse(parts[0], out var min)
            && int.TryParse(parts[1], out var sec)
            ? TimeSpan.FromSeconds(min * 60 + sec)
            : TimeSpan.FromMinutes(3);
    }

    // Deterministic Guid from a Discogs integer ID to prevent duplicate imports
    private static Guid DiscogsGuid(int id) => new Guid(id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // --- JSON DTOs ---

    private sealed record SearchResponse(
        [property: JsonPropertyName("results")] List<SearchResultDto> Results);

    private sealed record SearchResultDto(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("type")] string Type);

    private sealed record ArtistReleasesResponse(
        [property: JsonPropertyName("releases")] List<ArtistReleaseDto> Releases);

    private sealed record ArtistReleaseDto(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("year")] int? Year,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("role")] string Role);

    private sealed record MasterResponse(
        [property: JsonPropertyName("tracklist")] List<TrackDto> Tracklist);

    private sealed record TrackDto(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("duration")] string? Duration,
        [property: JsonPropertyName("type_")] string? Type_);
}
