using ActualGameSearch.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ActualGameSearch.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameSearch(this IServiceCollection services, string? dbPath = null)
    {
        dbPath ??= ActualGameSearch.Core.AppConfig.DbPath;
        if (File.Exists(dbPath))
            services.AddSingleton<IGameRepository>(_ => new SqliteGameRepository(dbPath));
        else
            services.AddSingleton<IGameRepository, InMemoryGameRepository>();

        services.AddSingleton<IEmbeddingProvider, DeterministicEmbeddingProvider>();
        services.AddSingleton<IGameSearchService, SimpleGameSearchService>();
        return services;
    }
}
