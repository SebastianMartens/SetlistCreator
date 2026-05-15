using Microsoft.Extensions.DependencyInjection;
using SetlistCreator.Backend.Services;

namespace SetlistCreator.Backend.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSetlistServices(this IServiceCollection services)
    {
        services.AddScoped<ISetlistService, InMemorySetlistService>();
        return services;
    }
}
