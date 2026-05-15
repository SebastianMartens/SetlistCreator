using LiteDB;
using SetlistCreator.Backend.Models;

namespace SetlistCreator.Backend.Services;

public sealed class LiteDbSetlistService : ISetlistService
{
    private const string CollectionName = "setlists";
    private readonly InMemorySetlistService _baseService = new();
    private readonly string _connectionString;

    public LiteDbSetlistService(string? databasePath = null)
    {
        var resolvedPath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "setlists.db");
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Filename={resolvedPath};Connection=shared";
    }

    public IReadOnlyList<Album> GetAlbums() => _baseService.GetAlbums();

    public IReadOnlyList<Song> GetSongsForAlbum(Guid albumId) => _baseService.GetSongsForAlbum(albumId);

    public Song CreateManualSong(string title, TimeSpan duration) => _baseService.CreateManualSong(title, duration);

    public void AddSong(Setlist setlist, Song song) => _baseService.AddSong(setlist, song);

    public void AddBreak(Setlist setlist, string name, TimeSpan duration) => _baseService.AddBreak(setlist, name, duration);

    public TimeSpan CalculateTotalDuration(Setlist setlist) => _baseService.CalculateTotalDuration(setlist);

    public Guid SaveSetlist(Setlist setlist)
    {
        ArgumentNullException.ThrowIfNull(setlist);

        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        collection.Upsert(ToDocument(setlist));
        return setlist.Id;
    }

    public IReadOnlyList<Setlist> GetSavedSetlists()
    {
        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        return collection.FindAll().Select(ToModel).ToList();
    }

    public Setlist GetSavedSetlist(Guid setlistId)
    {
        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        var source = collection.FindById(setlistId)
            ?? throw new KeyNotFoundException($"No setlist with id '{setlistId}' was found.");

        return ToModel(source);
    }

    public Setlist CopySavedSetlist(Guid setlistId)
    {
        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        var source = collection.FindById(setlistId)
            ?? throw new KeyNotFoundException($"No setlist with id '{setlistId}' was found.");

        var copy = new SetlistDocument
        {
            Id = Guid.NewGuid(),
            VenueName = source.VenueName,
            GigDate = source.GigDate,
            Items = source.Items.Select(item => new SetlistItemDocument
            {
                Type = item.Type,
                Name = item.Name,
                DurationTicks = item.DurationTicks
            }).ToList()
        };

        collection.Insert(copy);
        return ToModel(copy);
    }

    public void UpdateGigInfo(Guid setlistId, string venueName, DateOnly? gigDate)
    {
        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        var setlist = collection.FindById(setlistId)
            ?? throw new KeyNotFoundException($"No setlist with id '{setlistId}' was found.");

        setlist.VenueName = venueName?.Trim() ?? string.Empty;
        setlist.GigDate = gigDate?.ToDateTime(TimeOnly.MinValue);
        collection.Update(setlist);
    }

    public void DeleteSavedSetlist(Guid setlistId)
    {
        using var database = new LiteDatabase(_connectionString);
        var collection = database.GetCollection<SetlistDocument>(CollectionName);
        collection.Delete(setlistId);
    }

    private static SetlistDocument ToDocument(Setlist setlist)
    {
        return new SetlistDocument
        {
            Id = setlist.Id,
            VenueName = setlist.VenueName?.Trim() ?? string.Empty,
            GigDate = setlist.GigDate?.ToDateTime(TimeOnly.MinValue),
            Items = setlist.Items.Select(item => new SetlistItemDocument
            {
                Type = item.Type,
                Name = item.Name,
                DurationTicks = item.Duration.Ticks
            }).ToList()
        };
    }

    private static Setlist ToModel(SetlistDocument document)
    {
        var setlist = new Setlist
        {
            Id = document.Id,
            VenueName = document.VenueName ?? string.Empty,
            GigDate = document.GigDate is null ? null : DateOnly.FromDateTime(document.GigDate.Value)
        };

        foreach (var item in document.Items)
        {
            setlist.Items.Add(new SetlistItem(item.Type, item.Name, TimeSpan.FromTicks(item.DurationTicks)));
        }

        return setlist;
    }

    private sealed class SetlistDocument
    {
        [BsonId]
        public Guid Id { get; init; }

        public string VenueName { get; set; } = string.Empty;

        public DateTime? GigDate { get; set; }

        public List<SetlistItemDocument> Items { get; set; } = [];
    }

    private sealed class SetlistItemDocument
    {
        public SetlistItemType Type { get; set; }

        public string Name { get; set; } = string.Empty;

        public long DurationTicks { get; set; }
    }
}
