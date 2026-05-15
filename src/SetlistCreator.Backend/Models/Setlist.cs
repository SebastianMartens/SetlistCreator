namespace SetlistCreator.Backend.Models;

public sealed class Setlist
{
    public IList<SetlistItem> Items { get; } = [];
}
