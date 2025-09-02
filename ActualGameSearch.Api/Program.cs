using ActualGameSearch.Core;
using ActualGameSearch.Core.Services;
using ActualGameSearch.Core.Models;
using ActualGameSearch.Steam;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Logging;
using ActualGameSearch.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
builder.Services.AddServiceObservability("ActualGameSearch.Api");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddGameSearch();

// Manifest + model dimension validation (best-effort; non-fatal warning if mismatch)
builder.Services.AddHostedService(sp => new ActualGameSearch.Api.ManifestValidationHostedService(sp.GetRequiredService<ILogger<ActualGameSearch.Api.ManifestValidationHostedService>>()));
builder.Services.AddHostedService<ActualGameSearch.Api.EtlOnStartHostedService>();

// Resilient HttpClient for Steam
builder.Services.AddHttpClient<SteamClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Removed weather placeholder endpoint.

// Lightweight health / liveness endpoint (Aspire-friendly)
app.MapGet("/health", () => Results.Ok(new { status = "ok", lastSearchLatencyMs = ActualGameSearch.Core.Services.SearchTelemetry.GetLastLatency() }))
    .WithName("Health");

// Game ingestion endpoint
app.MapPost("/api/games", (GameIngestRequest req, IGameRepository repo, ILoggerFactory lf) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Description))
        return Results.BadRequest(Result.Fail<GameDto>("Name and Description required","validation"));
    var game = Game.CreateNew(req.Name, req.Description, req.Tags, req.IsAdult);
    repo.Add(game);
    lf.CreateLogger("Ingest").LogInformation("Ingested game {Id} name={Name} tags={TagsCount}", game.Id, game.Name, game.Tags.Count);
    return Results.Created($"/api/games/{game.Id}", Result.Ok(new GameDto(game.Id, game.Name, game.Description, game.Tags, game.IsAdult)));
});

// Single game get
app.MapGet("/api/games/{id:guid}", (Guid id, IGameRepository repo, ILogger<Program> log) =>
{
    var g = repo.Get(id);
    if (g is null)
    {
        log.LogWarning("Game {Id} not found", id);
        return Results.NotFound(Result.Fail<GameDto>($"Game {id} not found","not_found"));
    }
    log.LogDebug("Fetched game {Id}", id);
    return Results.Ok(Result.Ok(new GameDto(g.Id, g.Name, g.Description, g.Tags, g.IsAdult)));
});

// Simple search endpoint
app.MapGet("/api/games/search", (string q, int take, bool includeAdult, IGameSearchService search, ILogger<Program> log) =>
{
    if (string.IsNullOrWhiteSpace(q)) return Results.BadRequest(Result.Fail<List<GameSearchResultDto>>("Query required","validation"));
    if (take <= 0) take = 10;
    var results = search.Search(q, take)
        .Where(r => includeAdult || !r.Game.IsAdult)
        .Select(r => new GameSearchResultDto(new GameDto(r.Game.Id, r.Game.Name, r.Game.Description, r.Game.Tags, r.Game.IsAdult), r.Score))
        .ToList();
    log.LogInformation("Search q='{Q}' take={Take} results={Count}", q, take, results.Count);
    return Results.Ok(Result.Ok(results));
});

// Dataset manifest (if present next to DB)
app.MapGet("/api/dataset/manifest", (ILogger<Program> log) =>
{
    var candidate = Path.Combine(AppContext.BaseDirectory, "manifest.json");
    if (!File.Exists(candidate))
    {
        log.LogWarning("Manifest not found at {Path}", candidate);
        return Results.NotFound(Result.Fail<object>("Manifest not found","not_found"));
    }
    var json = File.ReadAllText(candidate);
    log.LogDebug("Serving manifest size={Bytes}", json.Length);
    return Results.Ok(Result.Ok(System.Text.Json.JsonDocument.Parse(json).RootElement));
});

// Embedding debug endpoint (parity harness uses this)
app.MapGet("/api/embedding/debug", (string text, IEmbeddingProvider provider, ILogger<Program> log) =>
{
    var vec = provider.Embed(text ?? string.Empty);
    log.LogInformation("Embedding debug len={Len} textLen={TextLen}", vec.Length, (text ?? string.Empty).Length);
    return Results.Ok(Result.Ok(new { dimension = provider.Dimension, vector = vec }));
});

// Canonical text endpoint: provides server-side composed text for a game (parity check)
app.MapGet("/api/embedding/canonical-text/{id:guid}", (Guid id, IGameRepository repo, ILogger<Program> log) =>
{
    var g = repo.Get(id);
    if (g is null) return Results.NotFound(Result.Fail<object>("Game not found","not_found"));
    var text = ActualGameSearch.Core.Services.GameTextComposer.Compose(g.Name, g.Description, g.Tags);
    log.LogDebug("Canonical text requested for {Id} length={Len}", id, text.Length);
    return Results.Ok(Result.Ok(new { id, text }));
});

// Steam random sample app ids (lightweight)
app.MapGet("/api/steam/sample-appids", async (int take, SteamClient steam, ILogger<Program> log, CancellationToken ct) =>
{
    if (take <= 0) take = 5;
    var all = await steam.GetAllAppsAsync(ct);
    if (all.Count == 0) return Results.Problem("Steam app list empty", statusCode: 502);
    var rand = System.Random.Shared;
    var sample = all.OrderBy(_ => rand.Next()).Take(take).ToList();
    log.LogInformation("Steam sample-appids take={Take} sourceCount={Source} returned={Returned}", take, all.Count, sample.Count);
    return Results.Ok(Result.Ok(sample));
});

app.Run();

// ----- Types declared after top-level statements (allowed) -----
// (WeatherForecast placeholder removed)
record GameIngestRequest(string Name, string Description, List<string> Tags, bool IsAdult = false);
record GameDto(Guid Id, string Name, string Description, IReadOnlyList<string> Tags, bool IsAdult);
record GameSearchResultDto(GameDto Game, double Score);
