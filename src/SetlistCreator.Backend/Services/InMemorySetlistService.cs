using SetlistCreator.Backend.Models;

namespace SetlistCreator.Backend.Services;

public sealed class InMemorySetlistService : ISetlistService
{
    private readonly List<Album> _albums;
    private readonly List<Setlist> _savedSetlists = [];

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

    public void ImportAlbum(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        _albums.RemoveAll(a => a.Id == album.Id);
        _albums.Add(album);
    }

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

    public Guid SaveSetlist(Setlist setlist)
    {
        ArgumentNullException.ThrowIfNull(setlist);

        _savedSetlists.RemoveAll(saved => saved.Id == setlist.Id);
        _savedSetlists.Add(Clone(setlist));
        return setlist.Id;
    }

    public IReadOnlyList<Setlist> GetSavedSetlists() => _savedSetlists.Select(Clone).ToList();

    public Setlist GetSavedSetlist(Guid setlistId)
    {
        var source = _savedSetlists.FirstOrDefault(setlist => setlist.Id == setlistId)
            ?? throw new KeyNotFoundException($"No setlist with id '{setlistId}' was found.");

        return Clone(source);
    }

    public Setlist CopySavedSetlist(Guid setlistId)
    {
        var source = GetSavedSetlist(setlistId);

        var copy = Clone(source);
        copy.Id = Guid.NewGuid();
        _savedSetlists.Add(Clone(copy));
        return copy;
    }

    public void UpdateGigInfo(Guid setlistId, string venueName, DateOnly? gigDate)
    {
        var setlist = _savedSetlists.FirstOrDefault(saved => saved.Id == setlistId)
            ?? throw new KeyNotFoundException($"No setlist with id '{setlistId}' was found.");

        setlist.VenueName = venueName?.Trim() ?? string.Empty;
        setlist.GigDate = gigDate;
    }

    public void DeleteSavedSetlist(Guid setlistId) => _savedSetlists.RemoveAll(saved => saved.Id == setlistId);

    private static Setlist Clone(Setlist setlist)
    {
        var clone = new Setlist
        {
            Id = setlist.Id,
            VenueName = setlist.VenueName,
            GigDate = setlist.GigDate
        };

        foreach (var item in setlist.Items)
        {
            clone.Items.Add(item with { });
        }

        return clone;
    }
}
