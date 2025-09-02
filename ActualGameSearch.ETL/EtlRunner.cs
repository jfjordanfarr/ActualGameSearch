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

    public static async Task<Result> RunAsync(ILogger? log = null, CancellationToken cancellationToken = default)
    {
        // Resolve base directory (invoker working dir)
        var baseDir = AppContext.BaseDirectory;
    var dbPath = Core.AppConfig.DbPath; // central path
        if (File.Exists(dbPath)) File.Delete(dbPath);

        int gameCount;
        var http = new HttpClient();
        var steamClient = new SteamClient(http, Environment.GetEnvironmentVariable("STEAM_API_KEY"), Microsoft.Extensions.Logging.Abstractions.NullLogger<SteamClient>.Instance);
        var cachePath = Path.Combine(baseDir, "apps-cache.json");
        List<SteamClient.SteamAppId> allApps;
        bool loadedFromCache = false;
        if (!AppConfig.EtlForceRefresh && File.Exists(cachePath))
        {
            try
            {
                using var fs = File.OpenRead(cachePath);
                var cached = JsonSerializer.Deserialize<List<SteamClient.SteamAppId>>(fs);
                if (cached is not null && cached.Count > 0)
                {
                    allApps = cached;
                    loadedFromCache = true;
                    log?.LogInformation("Loaded cached Steam app list (count={Count}). Set ETL_FORCE_REFRESH=1 to refresh.", allApps.Count);
                }
                else
                {
                    log?.LogInformation("Cache empty; fetching app list (Steam)...");
                    allApps = await steamClient.GetAllAppsAsync();
                }
            }
            catch
            {
                log?.LogWarning("Failed reading cache; fetching app list (Steam)...");
                allApps = await steamClient.GetAllAppsAsync();
            }
        }
        else
        {
            log?.LogInformation(AppConfig.EtlForceRefresh ? "Force refresh enabled; fetching app list (Steam)..." : "Fetching app list (Steam)...");
            allApps = await steamClient.GetAllAppsAsync();
        }

        if (!loadedFromCache)
        {
            try
            {
                File.WriteAllText(cachePath, JsonSerializer.Serialize(allApps, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch (Exception ex)
            {
                log?.LogWarning(ex, "Failed to write app list cache");
            }
        }

        var rnd = AppConfig.EtlRandomSeed.HasValue ? new Random(AppConfig.EtlRandomSeed.Value) : Random.Shared;
        int sampleSize = AppConfig.EtlSampleSize;
    var sampledAppIds = allApps.Where(a => !string.IsNullOrWhiteSpace(a.Name))
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

            var provider = new DeterministicEmbeddingProvider();
            var fetched = new List<(string name, string desc, string[] tags, bool isAdult)>();
            foreach (var appId in sampledAppIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var details = await steamClient.GetAppDetailsAsync(appId);
                    if (details is null) continue;
                    var reviews = await steamClient.GetReviewsAsync(appId, count: 10, language: "english");
                    File.WriteAllText(Path.Combine(rawDir, $"{appId}_details.json"), JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true }));
                    if (reviews is not null)
                        File.WriteAllText(Path.Combine(rawDir, $"{appId}_reviews.json"), JsonSerializer.Serialize(reviews, new JsonSerializerOptions { WriteIndented = true }));
                    var reviewText = reviews is null ? string.Empty : string.Join(" \n", reviews.Reviews.Select(r => r.ReviewText));
                    var tags = details.Categories.Concat(details.Genres).Select(t => t.ToLowerInvariant()).Distinct().Take(12).ToArray();
                    var desc = string.IsNullOrWhiteSpace(details.ShortDescription) ? details.DetailedDescription : details.ShortDescription;
                    var combined = desc + "\n" + string.Join(' ', reviewText.Split(' ').Take(120));
                    fetched.Add((details.Name, combined, tags, false));
                }
                catch (Exception ex)
                {
                    log?.LogWarning(ex, "Failed fetching app {AppId}", appId);
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
            cmd.CommandText = "SELECT Vector FROM Embeddings LIMIT 5";
            using var r = cmd.ExecuteReader();
            using var sha = SHA256.Create();
            while (r.Read())
            {
                var bytes = (byte[])r[0];
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            embeddingsSampleSha = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
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
    var manifest = DatasetManifest.Placeholder() with { GameCount = gameCount, DbSha256 = dbSha, EmbeddingsSampleSha256 = embeddingsSampleSha, AppListSha256 = appListHash, SampledAppIds = sampledCount, SampleSeed = AppConfig.EtlRandomSeed };
        var manifestPath = Core.AppConfig.ManifestPath;
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        log?.LogInformation("ETL complete. Games={Games} DbHash={Hash}", gameCount, dbSha);
        return new Result(dbPath, manifestPath, dbSha, gameCount);
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
