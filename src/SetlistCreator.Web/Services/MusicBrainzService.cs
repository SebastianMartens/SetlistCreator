using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SetlistCreator.Backend.Models;

namespace SetlistCreator.Web.Services;

public sealed class MusicBrainzService : IMusicSearchService
{
    private readonly HttpClient _http;

    public MusicBrainzService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<ArtistResult>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<ArtistSearchResponse>(
            $"artist?query={Uri.EscapeDataString(query)}&fmt=json&limit=10", cancellationToken);

        return response?.Artists
            .Select(a => new ArtistResult(a.Id, a.Name, a.Disambiguation))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AlbumResult>> GetArtistAlbumsAsync(string artistMbid, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<ReleaseSearchResponse>(
            $"release?artist={artistMbid}&type=album&status=official&fmt=json&limit=100", cancellationToken);

        return response?.Releases
            .Select(r => new AlbumResult(r.Id, r.Title, r.Date?.Length >= 4 ? r.Date[..4] : null))
            .OrderBy(r => r.Year)
            .ToList() ?? [];
    }

    public async Task<Album> GetAlbumWithTracksAsync(string releaseMbid, string albumTitle, CancellationToken cancellationToken = default)
    {
        var release = await _http.GetFromJsonAsync<ReleaseDetailResponse>(
            $"release/{releaseMbid}?inc=recordings&fmt=json", cancellationToken);

        var songs = release?.Media
            .SelectMany(m => m.Tracks)
            .Select(t => new Song(
                Guid.NewGuid(),
                t.Title,
                t.Length is > 0 ? TimeSpan.FromMilliseconds(t.Length.Value) : TimeSpan.FromMinutes(3),
                albumTitle))
            .ToList() ?? [];

        return new Album(Guid.Parse(releaseMbid), albumTitle, songs);
    }

    // --- JSON DTOs ---

    private sealed record ArtistSearchResponse(
        [property: JsonPropertyName("artists")] List<ArtistDto> Artists);

    private sealed record ArtistDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("disambiguation")] string? Disambiguation);

    private sealed record ReleaseSearchResponse(
        [property: JsonPropertyName("releases")] List<ReleaseDto> Releases);

    private sealed record ReleaseDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("date")] string? Date);

    private sealed record ReleaseDetailResponse(
        [property: JsonPropertyName("media")] List<MediaDto> Media);

    private sealed record MediaDto(
        [property: JsonPropertyName("tracks")] List<TrackDto> Tracks);

    private sealed record TrackDto(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("length")] int? Length);
}
