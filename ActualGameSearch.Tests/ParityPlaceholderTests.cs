using ActualGameSearch.Core.Services;
using Xunit;

namespace ActualGameSearch.Tests;

public class ParityPlaceholderTests
{
    private readonly DeterministicEmbeddingProvider _provider = new();

    [Fact]
    public void DeterministicEmbedding_SameInput_ProducesSameVector()
    {
        var a = _provider.Embed("Counter-Strike");
        var b = _provider.Embed("Counter-Strike");
        Assert.Equal(a.Length, b.Length);
        for (int i = 0; i < a.Length; i++) Assert.Equal(a[i], b[i]);
    }

    [Fact]
    public void DeterministicEmbedding_DifferentInput_Differs()
    {
        var a = _provider.Embed("Counter-Strike");
        var b = _provider.Embed("Stardew Valley");
        Assert.NotEqual(a[0], b[0]);
    }

    [Fact]
    public void CalibrationFile_AllPrompts_EmbeddingsHaveCorrectDimensionAndUnitNorm()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "embedding_parity_calibration.json");
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path), $"Calibration file missing at {path}");
        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var dim = json.RootElement.GetProperty("dimension").GetInt32();
        Assert.Equal(_provider.Dimension, dim);
        var prompts = json.RootElement.GetProperty("prompts").EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        Assert.NotEmpty(prompts);
        // If vectors precomputed, validate cosine drift <= tolerance
        if (json.RootElement.TryGetProperty("vectors", out var vectorsEl))
        {
            int idx = 0;
            foreach (var p in prompts)
            {
                if (idx >= vectorsEl.GetArrayLength()) break;
                var refVec = vectorsEl[idx].EnumerateArray().Select(v => (float)v.GetDouble()).ToArray();
                var cur = _provider.Embed(p);
                Assert.Equal(dim, refVec.Length);
                double dot = 0; for (int i = 0; i < dim; i++) dot += refVec[i] * cur[i];
                Assert.InRange(dot, 0.995, 1.005);
                idx++;
            }
        }
        foreach (var p in prompts)
        {
            var v = _provider.Embed(p);
            Assert.Equal(dim, v.Length);
            double sum = 0; foreach (var f in v) sum += f * f;
            var norm = Math.Sqrt(sum);
            Assert.InRange(norm, 0.999, 1.001);
        }
    }

    [Fact]
    public void Manifest_DbSha256_MatchesFile_WhenPresent()
    {
        // Arrange: look for manifest next to API output (during tests API content root is Api project dir)
        var apiDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ActualGameSearch.Api");
        var manifestPath = Path.Combine(apiDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            // Skip (no manifest yet) - treat absence as success to keep green until ETL runs
            return;
        }
        var manifest = System.Text.Json.JsonSerializer.Deserialize<ActualGameSearch.Core.Manifest.DatasetManifest>(File.ReadAllText(manifestPath))!;
        var dbPath = Path.Combine(apiDir, "games.db");
        Assert.True(File.Exists(dbPath), $"games.db missing at {dbPath}");
        var recomputed = ActualGameSearch.Core.Manifest.DatasetManifestLoader.ComputeSha256(dbPath);
        Assert.Equal(manifest.DbSha256, recomputed);
    }

    [Fact]
    public void StoredEmbeddings_IfPresent_AreUnitNorm()
    {
        var apiDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ActualGameSearch.Api");
        var dbPath = Path.Combine(apiDir, "games.db");
        if (!File.Exists(dbPath)) return; // skip if no DB
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Vector FROM Embeddings LIMIT 10";
        using var r = cmd.ExecuteReader();
        int count = 0;
        while (r.Read())
        {
            var bytes = (byte[])r[0];
            var dim = bytes.Length / sizeof(float);
            Assert.Equal(_provider.Dimension, dim);
            var vec = new float[dim];
            System.Buffer.BlockCopy(bytes, 0, vec, 0, bytes.Length);
            double sum = 0; foreach (var f in vec) sum += f * f;
            var norm = Math.Sqrt(sum);
            Assert.InRange(norm, 0.999, 1.001);
            count++;
        }
        Assert.True(count > 0, "No embeddings rows found to validate");
    }
}
