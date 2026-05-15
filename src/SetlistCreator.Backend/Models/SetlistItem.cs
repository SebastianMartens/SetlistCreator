namespace SetlistCreator.Backend.Models;

public sealed record SetlistItem(SetlistItemType Type, string Name, TimeSpan Duration)
{
    public static SetlistItem FromSong(Song song) => new(SetlistItemType.Song, song.Title, song.Duration);

    public static SetlistItem FromBreak(string name, TimeSpan duration) => new(SetlistItemType.Break, name, duration);
}
