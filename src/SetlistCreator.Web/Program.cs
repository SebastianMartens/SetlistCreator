using SetlistCreator.Backend.DependencyInjection;
using SetlistCreator.Web.Components;
using SetlistCreator.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSetlistServices();
builder.Services.AddHttpClient<MusicBrainzService>(client =>
{
    client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SetlistCreator/1.0 (+https://github.com/example/SetlistCreator)");
});
var discogsToken = builder.Configuration["Discogs:Token"];
builder.Services.AddHttpClient<DiscogsService>(client =>
{
    client.BaseAddress = new Uri("https://api.discogs.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SetlistCreator/1.0 (+https://github.com/example/SetlistCreator)");
    if (!string.IsNullOrWhiteSpace(discogsToken))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Discogs token={discogsToken}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
