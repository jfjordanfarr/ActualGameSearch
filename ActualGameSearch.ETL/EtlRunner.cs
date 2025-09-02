using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ActualGameSearch.Core.Manifest;
using ActualGameSearch.Core.Services;
using ActualGameSearch.Steam;
using ActualGameSearch.Core; // AppConfig

namespace ActualGameSearch.ETL;

public static class EtlRunner
{
    public sealed record Result(string DbPath, string ManifestPath, string DbSha256, int GameCount);

    public static Task<Result> RunAsync(ILogger? log = null, CancellationToken cancellationToken = default)
    {
        // Resolve base directory (invoker working dir)
        var baseDir = AppContext.BaseDirectory;
    var dbPath = Core.AppConfig.DbPath; // central path
        if (File.Exists(dbPath)) File.Delete(dbPath);

        int gameCount;
    var http = new HttpClient();
    List<(int AppId, string Name)> allApps;
    bool live = string.Equals(Environment.GetEnvironmentVariable("USE_STEAM_LIVE"), "true", StringComparison.OrdinalIgnoreCase);
    SteamClient? steamClient = null;
    if (live)
    {
        try
        {
            steamClient = new SteamClient(http, Environment.GetEnvironmentVariable("STEAM_API_KEY"), new LoggerFactory().CreateLogger<SteamClient>());
            var apps = steamClient.GetAllAppsAsync(cancellationToken).GetAwaiter().GetResult();
            allApps = apps.Where(a => !string.IsNullOrWhiteSpace(a.Name)).Select(a => (a.AppId, a.Name)).ToList();
            log?.LogInformation("Fetched live Steam app census count={Count}", allApps.Count);
        }
        catch (Exception ex)
        {
            log?.LogWarning(ex, "Falling back to synthetic app list (Steam fetch failed)");
            allApps = Enumerable.Range(1, 500).Select(i => (100000 + i, $"Placeholder Game {i}")) .ToList();
            live = false; // disable live path for remainder
        }
    }
    else
    {
        allApps = Enumerable.Range(1, 500).Select(i => (100000 + i, $"Placeholder Game {i}")) .ToList();
    }

        var rnd = AppConfig.EtlRandomSeed.HasValue ? new Random(AppConfig.EtlRandomSeed.Value) : Random.Shared;
        int sampleSize = AppConfig.EtlSampleSize;
    var sampledAppIds = allApps
        .OrderBy(_ => rnd.Next())
        .Take(sampleSize)
        .Select(a => a.AppId)
        .ToArray();
    log?.LogInformation("Sampled {Count} app ids (target {Target}).", sampledAppIds.Length, sampleSize);
        var rawDir = Path.Combine(baseDir, "raw-steam");
        Directory.CreateDirectory(rawDir);
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE Games (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    Tags TEXT NOT NULL,
    IsAdult INTEGER NOT NULL
);
CREATE TABLE Embeddings (
    GameId TEXT PRIMARY KEY REFERENCES Games(Id),
    Vector BLOB NOT NULL
);";
                cmd.ExecuteNonQuery();
            }

            // Select embedding provider (deterministic default, ONNX if enabled)
            IEmbeddingProvider provider;
            var useOnnx = string.Equals(Environment.GetEnvironmentVariable("USE_ONNX_EMBEDDINGS"), "true", StringComparison.OrdinalIgnoreCase);
            if (useOnnx)
            {
                provider = new OnnxEmbeddingProvider();
                log?.LogInformation("Using ONNX embedding provider for ETL (flag USE_ONNX_EMBEDDINGS=true).");
            }
            else
            {
                provider = new DeterministicEmbeddingProvider();
            }
            var fetched = new List<(string name, string desc, string[] tags, bool isAdult)>();
            foreach (var appId in sampledAppIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (live)
                {
                    try
                    {
                        if (steamClient is null)
                            steamClient = new SteamClient(http, Environment.GetEnvironmentVariable("STEAM_API_KEY"), new LoggerFactory().CreateLogger<SteamClient>());
                        var details = steamClient.GetAppDetailsAsync(appId, cancellationToken).GetAwaiter().GetResult();
                        if (details is null) { continue; }
                        var reviews = steamClient.GetReviewsAsync(appId, count: 10, language: "english", cancellationToken).GetAwaiter().GetResult();
                        var reviewText = reviews is null ? string.Empty : string.Join(" \n", reviews.Reviews.Select(r => r.ReviewText));
                        var tags = details.Categories.Concat(details.Genres).Select(t => t.ToLowerInvariant()).Distinct().Take(12).ToArray();
                        var desc = string.IsNullOrWhiteSpace(details.ShortDescription) ? details.DetailedDescription : details.ShortDescription;
                        var combined = desc + "\n" + string.Join(' ', reviewText.Split(' ').Take(120));
                        fetched.Add((details.Name, combined, tags, false));
                    }
                    catch (Exception ex)
                    {
                        log?.LogWarning(ex, "Failed fetching live data for app {AppId}; using placeholder entry", appId);
                        fetched.Add(($"Placeholder Game {appId}", "Live fetch failed; placeholder description.", new[] { "placeholder", "game" }, false));
                    }
                }
                else
                {
                    var name = $"Placeholder Game {appId}";
                    var desc = "A placeholder description used for early embedding pipeline validation.";
                    var tags = new[] { "placeholder", "game" };
                    fetched.Add((name, desc, tags, false));
                }
            }
            if (fetched.Count == 0)
            {
                fetched.Add(("Fallback Sample", "Placeholder description.", new[] { "sample" }, false));
            }
            gameCount = fetched.Count;

            using var tx = conn.BeginTransaction();
            foreach (var (name, desc, tags, adult) in fetched)
            {
                var id = Guid.NewGuid();
                var text = GameTextComposer.Compose(name, desc, tags);
                var vec = provider.Embed(text);
                using (var ic = conn.CreateCommand())
                {
                    ic.CommandText = "INSERT INTO Games (Id,Name,Description,Tags,IsAdult) VALUES ($id,$n,$d,$t,$a)";
                    ic.Parameters.AddWithValue("$id", id.ToString());
                    ic.Parameters.AddWithValue("$n", name);
                    ic.Parameters.AddWithValue("$d", desc);
                    ic.Parameters.AddWithValue("$t", string.Join(',', tags));
                    ic.Parameters.AddWithValue("$a", adult ? 1 : 0);
                    ic.ExecuteNonQuery();
                }
                var bytes = new byte[vec.Length * sizeof(float)];
                Buffer.BlockCopy(vec, 0, bytes, 0, bytes.Length);
                using (var ie = conn.CreateCommand())
                {
                    ie.CommandText = "INSERT INTO Embeddings (GameId,Vector) VALUES ($id,$v)";
                    ie.Parameters.AddWithValue("$id", id.ToString());
                    ie.Parameters.AddWithValue("$v", bytes);
                    ie.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }

        var dbSha = DatasetManifestLoader.ComputeSha256(dbPath);
        string embeddingsSampleSha;
        try
        {
            using var conn2 = new SqliteConnection($"Data Source={dbPath}");
            conn2.Open();
            using var cmd = conn2.CreateCommand();
            // Deterministic ordering: first N by rowid
            cmd.CommandText = "SELECT Vector FROM Embeddings ORDER BY rowid ASC LIMIT 8";
            using var r = cmd.ExecuteReader();
            using var sha = SHA256.Create();
            int count = 0;
            while (r.Read())
            {
                var bytes = (byte[])r[0];
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
                count++;
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            embeddingsSampleSha = count > 0 ? Convert.ToHexString(sha.Hash!).ToLowerInvariant() : string.Empty;
        }
        catch { embeddingsSampleSha = string.Empty; }

        string appListHash;
        try
        {
            using var shaList = SHA256.Create();
            // Hash app list for reproducibility
            foreach (var a in allApps.OrderBy(a => a.AppId))
            {
                var line = System.Text.Encoding.UTF8.GetBytes($"{a.AppId}|{a.Name}\n");
                shaList.TransformBlock(line, 0, line.Length, null, 0);
            }
            shaList.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            appListHash = Convert.ToHexString(shaList.Hash!).ToLowerInvariant();
        }
        catch { appListHash = string.Empty; }

    var sampledCount = sampledAppIds.Length;
        string? modelFileHash = null;
        string? tokenizerFileHash = null;
        try
        {
            var modelPath = Environment.GetEnvironmentVariable("ACTUALGAME_MODEL_PATH");
            if (!string.IsNullOrWhiteSpace(modelPath) && File.Exists(modelPath))
                modelFileHash = DatasetManifestLoader.ComputeSha256(modelPath);
            var tokPath = Environment.GetEnvironmentVariable("ACTUALGAME_TOKENIZER_VOCAB");
            if (!string.IsNullOrWhiteSpace(tokPath) && File.Exists(tokPath))
                tokenizerFileHash = DatasetManifestLoader.ComputeSha256(tokPath);
        }
        catch { /* ignore hash failures */ }
    var manifest = DatasetManifest.Placeholder() with { GameCount = gameCount, DbSha256 = dbSha, EmbeddingsSampleSha256 = embeddingsSampleSha, AppListSha256 = appListHash, SampledAppIds = sampledCount, SampleSeed = AppConfig.EtlRandomSeed, ModelFileSha256 = modelFileHash, TokenizerFileSha256 = tokenizerFileHash };
        var manifestPath = Core.AppConfig.ManifestPath;
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        log?.LogInformation("ETL complete. Games={Games} DbHash={Hash}", gameCount, dbSha);
    return Task.FromResult(new Result(dbPath, manifestPath, dbSha, gameCount));
    }

    /// <summary>
    /// Export embeddings from existing DB to JSON (GameId, vector[]) limited to top count.
    /// </summary>
    public static void ExportEmbeddings(string outputPath, int top = 20, ILogger? log = null)
    {
        var dbPath = Core.AppConfig.DbPath;
        if (!File.Exists(dbPath)) throw new FileNotFoundException("DB not found", dbPath);
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT GameId, Vector FROM Embeddings LIMIT $limit";
        cmd.Parameters.AddWithValue("$limit", top);
        using var r = cmd.ExecuteReader();
        var list = new List<object>();
        int dim = -1;
        while (r.Read())
        {
            var id = r.GetString(0);
            var bytes = (byte[])r[1];
            dim = bytes.Length / sizeof(float);
            var vec = new float[dim];
            Buffer.BlockCopy(bytes, 0, vec, 0, bytes.Length);
            list.Add(new { id, vector = vec });
        }
        var json = JsonSerializer.Serialize(new { dimension = dim, count = list.Count, data = list }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
        log?.LogInformation("Exported {Count} embeddings (dim={Dim}) to {Path}", list.Count, dim, outputPath);
    }
}
