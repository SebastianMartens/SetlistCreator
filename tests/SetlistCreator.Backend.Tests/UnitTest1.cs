using SetlistCreator.Backend.Models;
using SetlistCreator.Backend.Services;

namespace SetlistCreator.Backend.Tests;

public sealed class UnitTest1
{
    private readonly InMemorySetlistService _service = new();

    [Fact]
    public void CalculateTotalDuration_IncludesSongsAndBreaks()
    {
        var setlist = new Setlist();
        var firstAlbum = _service.GetAlbums().First();
        var song = _service.GetSongsForAlbum(firstAlbum.Id).First();

        _service.AddSong(setlist, song);
        _service.AddBreak(setlist, "Intermission", TimeSpan.FromMinutes(15));

        var total = _service.CalculateTotalDuration(setlist);

        Assert.Equal(song.Duration + TimeSpan.FromMinutes(15), total);
    }

    [Fact]
    public void CreateManualSong_RequiresNonEmptyTitleAndPositiveDuration()
    {
        Assert.Throws<ArgumentException>(() => _service.CreateManualSong(" ", TimeSpan.FromMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CreateManualSong("Manual", TimeSpan.Zero));
    }

    [Fact]
    public void GetSongsForAlbum_ReturnsSongsForExistingAlbum()
    {
        var album = _service.GetAlbums().First();

        var songs = _service.GetSongsForAlbum(album.Id);

        Assert.NotEmpty(songs);
        Assert.All(songs, song => Assert.Equal(album.Name, song.AlbumName));
    }
}
