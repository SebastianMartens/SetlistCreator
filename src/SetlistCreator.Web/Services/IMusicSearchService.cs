using SetlistCreator.Backend.Models;

namespace SetlistCreator.Web.Services;

public interface IMusicSearchService
{
    Task<IReadOnlyList<ArtistResult>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlbumResult>> GetArtistAlbumsAsync(string artistMbid, CancellationToken cancellationToken = default);

    Task<Album> GetAlbumWithTracksAsync(string releaseMbid, string albumTitle, CancellationToken cancellationToken = default);
}

public sealed record ArtistResult(string Id, string Name, string? Disambiguation);

public sealed record AlbumResult(string Id, string Title, string? Year);
