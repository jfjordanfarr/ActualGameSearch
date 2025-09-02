using System.Net.Http.Json;
using System.Text.Json;
using ActualGameSearch.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ActualGameSearch.Tests;

public class ParityIntegrationTests : IClassFixture<WebApplicationFactory<ActualGameSearch.Api.ProgramMarker>>
{
    private readonly WebApplicationFactory<ActualGameSearch.Api.ProgramMarker> _factory;
    private readonly DeterministicEmbeddingProvider _provider = new();

    public ParityIntegrationTests(WebApplicationFactory<ActualGameSearch.Api.ProgramMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task CanonicalText_And_DebugEmbedding_Parity()
    {
        var client = _factory.CreateClient();

        // Ingest a game first
        var ingest = new { Name = "Test Game", Description = "Some Desc", Tags = new[] { "tag1", "tag2" }, IsAdult = false };
        var respIngest = await client.PostAsJsonAsync("/api/games", ingest);
        respIngest.EnsureSuccessStatusCode();
        var ingestJson = await respIngest.Content.ReadFromJsonAsync<JsonElement>();
        var id = ingestJson.GetProperty("data").GetProperty("id").GetGuid();

        // Fetch canonical text
        var canonResp = await client.GetAsync($"/api/embedding/canonical-text/{id}");
        canonResp.EnsureSuccessStatusCode();
        var canonJson = await canonResp.Content.ReadFromJsonAsync<JsonElement>();
        var text = canonJson.GetProperty("data").GetProperty("text").GetString()!;

        // Server embedding via debug endpoint
        var dbgResp = await client.GetAsync($"/api/embedding/debug?text={Uri.EscapeDataString(text)}");
        dbgResp.EnsureSuccessStatusCode();
        var dbgJson = await dbgResp.Content.ReadFromJsonAsync<JsonElement>();
        var vecServer = dbgJson.GetProperty("data").GetProperty("vector").EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();

        // Local embedding
        var vecLocal = _provider.Embed(text);
        Assert.Equal(vecLocal.Length, vecServer.Length);
        for (int i = 0; i < vecLocal.Length; i++) Assert.Equal(vecLocal[i], vecServer[i], 6);

        // Cosine should be ~1
        double dot = 0; for (int i = 0; i < vecLocal.Length; i++) dot += vecLocal[i] * vecServer[i];
        Assert.InRange(dot, 0.9999, 1.0001);
    }

    [Fact]
    public async Task CalibrationPrompts_DebugEndpoint_Parity()
    {
        var client = _factory.CreateClient();
        // Load calibration file (shared at solution root under data/)
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "embedding_parity_calibration.json");
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path), $"Calibration file missing at {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var prompts = doc.RootElement.GetProperty("prompts").EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        Assert.NotEmpty(prompts);

        foreach (var p in prompts)
        {
            // Server embedding
            var dbgResp = await client.GetAsync($"/api/embedding/debug?text={Uri.EscapeDataString(p)}");
            dbgResp.EnsureSuccessStatusCode();
            var dbgJson = await dbgResp.Content.ReadFromJsonAsync<JsonElement>();
            var vecServer = dbgJson.GetProperty("data").GetProperty("vector").EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();

            // Local embedding
            var vecLocal = _provider.Embed(p);
            Assert.Equal(vecLocal.Length, vecServer.Length);
            for (int i = 0; i < vecLocal.Length; i++) Assert.Equal(vecLocal[i], vecServer[i], 6);
        }
    }
}
