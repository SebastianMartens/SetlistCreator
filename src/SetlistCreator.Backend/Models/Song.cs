namespace SetlistCreator.Backend.Models;

public sealed record Song(Guid Id, string Title, TimeSpan Duration, string? AlbumName = null);
