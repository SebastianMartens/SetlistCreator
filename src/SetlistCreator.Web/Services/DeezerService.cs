using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SetlistCreator.Backend.Models;

namespace SetlistCreator.Web.Services;

public sealed class DeezerService : IMusicSearchService
{
    private readonly HttpClient _http;

    public DeezerService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<ArtistResult>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<DataListResponse<ArtistDto>>(
            $"search/artist?q={Uri.EscapeDataString(query)}&limit=10", cancellationToken);

        return response?.Data
            .Select(a => new ArtistResult(a.Id.ToString(), a.Name, null))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AlbumResult>> GetArtistAlbumsAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<DataListResponse<AlbumDto>>(
            $"artist/{Uri.EscapeDataString(artistId)}/albums?limit=100", cancellationToken);

        return response?.Data
            .Where(a => a.RecordType == "album")
            .Select(a => new AlbumResult(
                a.Id.ToString(),
                a.Title,
                a.ReleaseDate?.Length >= 4 ? a.ReleaseDate[..4] : null))
            .OrderBy(a => a.Year)
            .ToList() ?? [];
    }

    public async Task<Album> GetAlbumWithTracksAsync(string albumId, string albumTitle, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<DataListResponse<TrackDto>>(
            $"album/{Uri.EscapeDataString(albumId)}/tracks", cancellationToken);

        var songs = response?.Data
            .Select(t => new Song(
                Guid.NewGuid(),
                t.Title,
                t.Duration > 0 ? TimeSpan.FromSeconds(t.Duration) : TimeSpan.FromMinutes(3),
                albumTitle))
            .ToList() ?? [];

        return new Album(DeezerGuid(long.Parse(albumId)), albumTitle, songs);
    }

    // Deterministic Guid from a Deezer integer ID to prevent duplicate imports
    private static Guid DeezerGuid(long id)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    // --- JSON DTOs ---

    private sealed record DataListResponse<T>(
        [property: JsonPropertyName("data")] List<T> Data);

    private sealed record ArtistDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name);

    private sealed record AlbumDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("record_type")] string? RecordType);

    private sealed record TrackDto(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("duration")] int Duration);
}
