using ActualGameSearch.Core.Models;
using Microsoft.Extensions.Logging;

namespace ActualGameSearch.Core.Services;

public interface IGameSearchService
{
    IEnumerable<GameSearchResult> Search(string query, int take = 10);
}

public sealed record GameSearchResult(Game Game, double Score);

internal sealed class SimpleGameSearchService(IGameRepository repository, IEmbeddingProvider embeddingProvider, ILogger<SimpleGameSearchService>? logger = null) : IGameSearchService
{
    public IEnumerable<GameSearchResult> Search(string query, int take = 10)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<GameSearchResult>();
        var source = SearchTelemetry.Source;
        using var activity = source.StartActivity("game.search", System.Diagnostics.ActivityKind.Internal);
        activity?.SetTag("search.query.length", query.Length);
        activity?.SetTag("search.take", take);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var q = embeddingProvider.Embed(query);
        double embedElapsedMs = sw.Elapsed.TotalMilliseconds;
        activity?.AddEvent(new System.Diagnostics.ActivityEvent("query.embedded", tags: new System.Diagnostics.ActivityTagsCollection(new[] { new KeyValuePair<string, object?>("elapsed.ms", embedElapsedMs) })));
        var games = repository.GetAll();
        int corpusCount = 0;
        var results = new List<GameSearchResult>();
        foreach (var g in games)
        {
            corpusCount++;
            float[] gv = repository.GetEmbedding(g.Id) ?? embeddingProvider.Embed(TextOf(g));
            var score = Cosine(q, gv);
            if (score > 0) results.Add(new GameSearchResult(g, score));
        }
        results = results.OrderByDescending(r => r.Score).Take(take).ToList();
        sw.Stop();
        activity?.SetTag("search.corpus.count", corpusCount);
        activity?.SetTag("search.results.count", results.Count);
        activity?.SetTag("search.elapsed.ms", sw.Elapsed.TotalMilliseconds);
        logger?.LogInformation("Search query='{Query}' take={Take} results={Count} corpus={Corpus} elapsedMs={Elapsed:F1}", query, take, results.Count, corpusCount, sw.Elapsed.TotalMilliseconds);
    SearchTelemetry.SearchCounter.Add(1);
    SearchTelemetry.ObserveLatency(sw.Elapsed.TotalMilliseconds);
        return results;
    }

    private static string TextOf(Game g) => GameTextComposer.Compose(g.Name, g.Description, g.Tags);

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0;
        for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];
        return dot; // vectors are expected normalized
    }

    // Embedding generation moved to injected provider; synchronous deterministic implementation.
}

public static class SearchTelemetry
{
    public static readonly System.Diagnostics.ActivitySource Source = new("ActualGameSearch.Core");
    private static readonly System.Diagnostics.Metrics.Meter Meter = new("ActualGameSearch.Core");
    public static readonly System.Diagnostics.Metrics.Counter<int> SearchCounter = Meter.CreateCounter<int>("game_search.requests");
    private static readonly System.Diagnostics.Metrics.Histogram<double> LatencyHistogram = Meter.CreateHistogram<double>("game_search.latency.ms", unit: "ms");
    private static long _lastLatencyMsTimes1000;
    public static void ObserveLatency(double ms)
    {
        LatencyHistogram.Record(ms);
        System.Threading.Interlocked.Exchange(ref _lastLatencyMsTimes1000, (long)(ms * 1000));
    }
    public static double GetLastLatency() => System.Threading.Interlocked.Read(ref _lastLatencyMsTimes1000) / 1000.0;
}
