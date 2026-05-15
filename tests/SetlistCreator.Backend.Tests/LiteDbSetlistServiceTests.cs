using SetlistCreator.Backend.Models;
using SetlistCreator.Backend.Services;

namespace SetlistCreator.Backend.Tests;

public sealed class LiteDbSetlistServiceTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveSetlist_PersistsVenueDateAndItems()
    {
        var service = new LiteDbSetlistService(_databasePath);
        var setlist = new Setlist
        {
            VenueName = "Grand Hall",
            GigDate = new DateOnly(2026, 6, 1)
        };

        var song = service.GetSongsForAlbum(service.GetAlbums().First().Id).First();
        service.AddSong(setlist, song);
        service.AddBreak(setlist, "Pause", TimeSpan.FromMinutes(10));

        service.SaveSetlist(setlist);

        var saved = service.GetSavedSetlists().Single();

        Assert.Equal("Grand Hall", saved.VenueName);
        Assert.Equal(new DateOnly(2026, 6, 1), saved.GigDate);
        Assert.Equal(2, saved.Items.Count);
    }

    [Fact]
    public void CopySavedSetlist_CreatesNewSetlist()
    {
        var service = new LiteDbSetlistService(_databasePath);
        var setlist = new Setlist { VenueName = "Arena" };
        var song = service.GetSongsForAlbum(service.GetAlbums().First().Id).First();
        service.AddSong(setlist, song);
        service.SaveSetlist(setlist);

        var copy = service.CopySavedSetlist(setlist.Id);
        var all = service.GetSavedSetlists();

        Assert.NotEqual(setlist.Id, copy.Id);
        Assert.Equal(2, all.Count);
        Assert.Equal(setlist.Items.Count, copy.Items.Count);
    }

    [Fact]
    public void UpdateAndDeleteSavedSetlist_ChangesPersistence()
    {
        var service = new LiteDbSetlistService(_databasePath);
        var setlist = new Setlist();
        var song = service.GetSongsForAlbum(service.GetAlbums().First().Id).First();
        service.AddSong(setlist, song);
        service.SaveSetlist(setlist);

        service.UpdateGigInfo(setlist.Id, "Open Air", new DateOnly(2026, 7, 10));
        var updated = service.GetSavedSetlists().Single();
        Assert.Equal("Open Air", updated.VenueName);
        Assert.Equal(new DateOnly(2026, 7, 10), updated.GigDate);

        service.DeleteSavedSetlist(setlist.Id);
        Assert.Empty(service.GetSavedSetlists());
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
