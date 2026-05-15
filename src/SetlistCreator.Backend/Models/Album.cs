namespace SetlistCreator.Backend.Models;

public sealed record Album(Guid Id, string Name, IReadOnlyList<Song> Songs);
