using SetlistCreator.Backend.Models;

namespace SetlistCreator.Backend.Services;

public interface ISetlistService
{
    IReadOnlyList<Album> GetAlbums();

    IReadOnlyList<Song> GetSongsForAlbum(Guid albumId);

    Song CreateManualSong(string title, TimeSpan duration);

    void AddSong(Setlist setlist, Song song);

    void AddBreak(Setlist setlist, string name, TimeSpan duration);

    TimeSpan CalculateTotalDuration(Setlist setlist);

    Guid SaveSetlist(Setlist setlist);

    IReadOnlyList<Setlist> GetSavedSetlists();

    Setlist GetSavedSetlist(Guid setlistId);

    Setlist CopySavedSetlist(Guid setlistId);

    void UpdateGigInfo(Guid setlistId, string venueName, DateOnly? gigDate);

    void DeleteSavedSetlist(Guid setlistId);
}
