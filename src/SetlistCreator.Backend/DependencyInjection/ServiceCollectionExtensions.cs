using Microsoft.Extensions.DependencyInjection;
using SetlistCreator.Backend.Services;

namespace SetlistCreator.Backend.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSetlistServices(this IServiceCollection services, string? databasePath = null)
    {
        services.AddScoped<ISetlistService>(_ => new LiteDbSetlistService(databasePath));
        return services;
    }
}
