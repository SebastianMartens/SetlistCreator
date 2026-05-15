using SetlistCreator.Backend.Models;

namespace SetlistCreator.Backend.Services;

public sealed class InMemorySetlistService : ISetlistService
{
    private readonly IReadOnlyList<Album> _albums;

    public InMemorySetlistService()
    {
        var firstAlbumSongs = new List<Song>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "City Lights", TimeSpan.FromMinutes(4.5), "First Light"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Echoes", TimeSpan.FromMinutes(5), "First Light"),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Falling Stars", TimeSpan.FromMinutes(3.75), "First Light")
        };

        var secondAlbumSongs = new List<Song>
        {
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Runaway Train", TimeSpan.FromMinutes(4), "Night Drive"),
            new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Neon Roads", TimeSpan.FromMinutes(4.25), "Night Drive"),
            new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Last Encore", TimeSpan.FromMinutes(6), "Night Drive")
        };

        _albums =
        [
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "First Light", firstAlbumSongs),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Night Drive", secondAlbumSongs)
        ];
    }

    public IReadOnlyList<Album> GetAlbums() => _albums;

    public IReadOnlyList<Song> GetSongsForAlbum(Guid albumId) => _albums.FirstOrDefault(a => a.Id == albumId)?.Songs ?? [];

    public Song CreateManualSong(string title, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");
        }

        return new Song(Guid.NewGuid(), title.Trim(), duration, null);
    }

    public void AddSong(Setlist setlist, Song song)
    {
        ArgumentNullException.ThrowIfNull(setlist);
        ArgumentNullException.ThrowIfNull(song);

        setlist.Items.Add(SetlistItem.FromSong(song));
    }

    public void AddBreak(Setlist setlist, string name, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(setlist);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Break name is required.", nameof(name));
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");
        }

        setlist.Items.Add(SetlistItem.FromBreak(name.Trim(), duration));
    }

    public TimeSpan CalculateTotalDuration(Setlist setlist)
    {
        ArgumentNullException.ThrowIfNull(setlist);

        return TimeSpan.FromTicks(setlist.Items.Sum(item => item.Duration.Ticks));
    }
}
