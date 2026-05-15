namespace SetlistCreator.Backend.Models;

public sealed class Setlist
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string VenueName { get; set; } = string.Empty;

    public DateOnly? GigDate { get; set; }

    public IList<SetlistItem> Items { get; } = [];
}
