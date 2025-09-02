namespace ActualGameSearch.Core;

/// <summary>
/// Centralized strongly-typed access to environment / config values (code-first defaults).
/// Read once; values can be forced to refresh by calling Reload (rarely needed).
/// </summary>
public static class AppConfig
{
    private static Dictionary<string,string?> _cache = new();
    private static readonly object _lock = new();

    private static string? Get(string key, string? def = null)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var v)) return v;
            v = Environment.GetEnvironmentVariable(key) ?? def;
            _cache[key] = v;
            return v;
        }
    }

    public static void Reload()
    {
        lock (_lock) _cache.Clear();
    }

    public static int EtlSampleSize
    {
        get
        {
            var raw = Get("ETL_SAMPLE_SIZE");
            return (int.TryParse(raw, out var n) && n > 0) ? Math.Min(n, 500) : 10; // soft cap 500
        }
    }

    public static bool EtlForceRefresh => (Get("ETL_FORCE_REFRESH") ?? string.Empty).Equals("1", StringComparison.OrdinalIgnoreCase);

    public static int? EtlRandomSeed
    {
        get
        {
            var raw = Get("ETL_RANDOM_SEED");
            if (int.TryParse(raw, out var n)) return n;
            return null;
        }
    }

    public static bool EnableConsoleTracing => (Get("OTEL_CONSOLE_EXPORTER") ?? string.Empty).Equals("1", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Central resolved database path. Environment variable ACTUAL_GAME_SEARCH_DB overrides; else fallback to base directory games.db.
    /// Always returns a fully-qualified absolute path (even if file does not yet exist).
    /// </summary>
    public static string DbPath
    {
        get
        {
            var raw = Get("ACTUAL_GAME_SEARCH_DB");
            string path = string.IsNullOrWhiteSpace(raw)
                ? Path.Combine(AppContext.BaseDirectory, "games.db")
                : raw!;
            return Path.GetFullPath(path);
        }
    }

    /// <summary>
    /// Path to dataset manifest (co-located with DbPath).
    /// </summary>
    public static string ManifestPath => Path.Combine(Path.GetDirectoryName(DbPath)!, "manifest.json");
}