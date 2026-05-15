using SetlistCreator.Backend.Models;

namespace SetlistCreator.Backend.Services;

public sealed class InMemorySetlistService : ISetlistService
{
    private readonly List<Album> _albums;
    private readonly List<Setlist> _savedSetlists = [];

    public InMemorySetlistService()
    {
        _albums = [];
    }

    public IReadOnlyList<Album> GetAlbums() => _albums;

    public IReadOnlyList<Song> GetSongsForAlbum(Guid albumId) => _albums.FirstOrDefault(a => a.Id == albumId)?.Songs ?? [];

    public void ImportAlbum(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        _albums.RemoveAll(a => a.Id == album.Id);
        _albums.Add(album);
    }

    public void RemoveAlbum(Guid albumId) => _albums.RemoveAll(a => a.Id == albumId);

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
