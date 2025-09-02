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

        // Embedding provider selection: deterministic (default) vs real ONNX (opt-in)
        var useOnnx = string.Equals(Environment.GetEnvironmentVariable("USE_ONNX_EMBEDDINGS"), "true", StringComparison.OrdinalIgnoreCase);
        // Auto-detect model.onnx if present and no explicit opt-out
        if (!useOnnx)
        {
            var autoModel = Path.Combine(AppContext.BaseDirectory, "models", "openclip", "model.onnx");
            if (File.Exists(autoModel)) useOnnx = true;
        }
        if (useOnnx)
            services.AddSingleton<IEmbeddingProvider, OnnxEmbeddingProvider>();
        else
            services.AddSingleton<IEmbeddingProvider, DeterministicEmbeddingProvider>();
        services.AddSingleton<IGameSearchService, SimpleGameSearchService>();
        return services;
    }
}
