using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SetlistCreator.Backend.Models;

namespace SetlistCreator.Web.Services;

public sealed class ITunesService : IMusicSearchService
{
    private readonly HttpClient _http;

    public ITunesService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<ArtistResult>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<SearchResponse>(
            $"search?term={Uri.EscapeDataString(query)}&entity=musicArtist&limit=10", cancellationToken);

        return response?.Results
            .Where(r => r.WrapperType == "artist" && r.ArtistId.HasValue)
            .Select(r => new ArtistResult(r.ArtistId!.Value.ToString(), r.ArtistName ?? string.Empty, null))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AlbumResult>> GetArtistAlbumsAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<SearchResponse>(
            $"lookup?id={Uri.EscapeDataString(artistId)}&entity=album&limit=200", cancellationToken);

        return response?.Results
            .Where(r => r.WrapperType == "collection" && r.CollectionType == "Album" && r.CollectionId.HasValue)
            .Select(r => new AlbumResult(
                r.CollectionId!.Value.ToString(),
                r.CollectionName ?? string.Empty,
                r.ReleaseDate?.Length >= 4 ? r.ReleaseDate[..4] : null))
            .OrderBy(r => r.Year)
            .ToList() ?? [];
    }

    public async Task<Album> GetAlbumWithTracksAsync(string collectionId, string albumTitle, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<SearchResponse>(
            $"lookup?id={Uri.EscapeDataString(collectionId)}&entity=song", cancellationToken);

        var songs = response?.Results
            .Where(r => r.WrapperType == "track" && !string.IsNullOrWhiteSpace(r.TrackName))
            .Select(t => new Song(
                Guid.NewGuid(),
                t.TrackName!,
                t.TrackTimeMillis is > 0 ? TimeSpan.FromMilliseconds(t.TrackTimeMillis.Value) : TimeSpan.FromMinutes(3),
                albumTitle))
            .ToList() ?? [];

        return new Album(ItunesGuid(long.Parse(collectionId)), albumTitle, songs);
    }

    // Deterministic Guid from an iTunes integer ID to prevent duplicate imports
    private static Guid ItunesGuid(long id)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    // --- JSON DTOs ---

    private sealed record SearchResponse(
        [property: JsonPropertyName("results")] List<ResultDto> Results);

    private sealed record ResultDto(
        [property: JsonPropertyName("wrapperType")] string? WrapperType,
        [property: JsonPropertyName("artistId")] long? ArtistId,
        [property: JsonPropertyName("artistName")] string? ArtistName,
        [property: JsonPropertyName("collectionId")] long? CollectionId,
        [property: JsonPropertyName("collectionName")] string? CollectionName,
        [property: JsonPropertyName("collectionType")] string? CollectionType,
        [property: JsonPropertyName("releaseDate")] string? ReleaseDate,
        [property: JsonPropertyName("trackName")] string? TrackName,
        [property: JsonPropertyName("trackTimeMillis")] long? TrackTimeMillis);
}
