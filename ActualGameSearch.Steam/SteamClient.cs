using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ActualGameSearch.Steam;

public class SteamClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly ILogger<SteamClient> _logger;
    private static readonly ActivitySource ActivitySource = new("ActualGameSearch.Steam");

    public SteamClient(HttpClient http, string? apiKey, ILogger<SteamClient> logger)
    {
        _http = http;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<AppDetailsResponse?> GetAppDetailsAsync(int appId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SteamClient.GetAppDetails");
        activity?.SetTag("steam.appid", appId);
        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=US&l=en"; // store API (no key required)
        _logger.LogDebug("Fetching app details for {AppId} from {Url}", appId, url);
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Non-success status {Status} fetching app {AppId}", resp.StatusCode, appId);
            return null;
        }
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var appEl)) return null;
        if (!appEl.TryGetProperty("success", out var successEl) || !successEl.GetBoolean()) return null;
        if (!appEl.TryGetProperty("data", out var dataEl)) return null;
        var model = AppDetailsResponseFactory.FromJson(dataEl);
        _logger.LogInformation("Fetched details for {AppId} name={Name} cats={Categories} genres={Genres}", appId, model.Name, model.Categories.Count, model.Genres.Count);
        return model;
    }

    public async Task<ReviewsResponse?> GetReviewsAsync(int appId, int count = 20, string? language = "all", CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SteamClient.GetReviews");
        activity?.SetTag("steam.appid", appId);
        var url = $"https://store.steampowered.com/appreviews/{appId}?json=1&filter=recent&language={language}&num_per_page={count}";
        _logger.LogDebug("Fetching reviews for {AppId} url={Url}", appId, url);
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogInformation("No reviews status={Status} app={AppId}", resp.StatusCode, appId);
            return null;
        }
        var text = await resp.Content.ReadAsStringAsync(ct);
        var rr = JsonSerializer.Deserialize<ReviewsRaw>(text);
        if (rr is null) return null;
        var model = ReviewsResponseFactory.FromRaw(appId, rr);
        _logger.LogInformation("Fetched {Count} reviews for {AppId}", model.Reviews.Count, appId);
        return model;
    }

    public async Task<List<SteamAppId>> GetAllAppsAsync(CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("SteamClient.GetAllApps");
        var url = "https://api.steampowered.com/ISteamApps/GetAppList/v2/"; // Public endpoint (no key required)
        _logger.LogInformation("Fetching full app list from Steam endpoint {Url}", url);
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Failed fetching app list status={Status}", resp.StatusCode);
            return new();
        }
        var text = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(text);
        if (!doc.RootElement.TryGetProperty("applist", out var ap) || !ap.TryGetProperty("apps", out var appsEl)) return new();
        var list = new List<SteamAppId>();
        foreach (var el in appsEl.EnumerateArray())
        {
            if (el.TryGetProperty("appid", out var idEl) && el.TryGetProperty("name", out var nameEl))
            {
                list.Add(new SteamAppId(idEl.GetInt32(), nameEl.GetString() ?? string.Empty));
            }
        }
        _logger.LogInformation("Fetched Steam app census count={Count}", list.Count);
        return list;
    }

    public record SteamAppId(int AppId, string Name);

    // Data Models
    public record AppDetailsResponse(int AppId, string Name, string ShortDescription, string DetailedDescription, string Type, bool IsFree, List<string> Categories, List<string> Genres, string? HeaderImage, string? Website);

    public record Review(int RecommendationId, string AuthorSteamId, int PlaytimeAtReview, int PlaytimeForever, string ReviewText, int VotesUp, int VotesFunny, bool VotedUp, DateTime TimeCreatedUtc);

    public record ReviewsResponse(int AppId, List<Review> Reviews);

    public record ReviewsRaw([property: JsonPropertyName("reviews")] List<ReviewEntry> Reviews);
    public record ReviewEntry(
        [property: JsonPropertyName("recommendationid")] string RecommendationId,
        [property: JsonPropertyName("author")] AuthorEntry Author,
        [property: JsonPropertyName("review")] string Review,
        [property: JsonPropertyName("votes_up")] int VotesUp,
        [property: JsonPropertyName("votes_funny")] int VotesFunny,
        [property: JsonPropertyName("voted_up")] bool VotedUp,
        [property: JsonPropertyName("timestamp_created")] long TimestampCreated
    );
    public record AuthorEntry(
        [property: JsonPropertyName("steamid")] string SteamId,
        [property: JsonPropertyName("playtime_at_review")] int PlaytimeAtReview,
        [property: JsonPropertyName("playtime_forever")] int PlaytimeForever
    );

    public static class AppDetailsResponseFactory
    {
        public static AppDetailsResponse FromJson(JsonElement data)
        {
            int appId = data.GetProperty("steam_appid").GetInt32();
            string name = data.GetProperty("name").GetString() ?? string.Empty;
            string shortDescription = data.GetProperty("short_description").GetString() ?? string.Empty;
            string detailed = data.GetProperty("detailed_description").GetString() ?? string.Empty;
            string type = data.GetProperty("type").GetString() ?? string.Empty;
            bool isFree = data.TryGetProperty("is_free", out var freeEl) && freeEl.GetBoolean();
            var categories = data.TryGetProperty("categories", out var catEl) ? catEl.EnumerateArray().Select(c => c.GetProperty("description").GetString() ?? string.Empty).Where(s => s.Length>0).ToList() : new();
            var genres = data.TryGetProperty("genres", out var genEl) ? genEl.EnumerateArray().Select(c => c.GetProperty("description").GetString() ?? string.Empty).Where(s => s.Length>0).ToList() : new();
            string? header = data.TryGetProperty("header_image", out var headerEl) ? headerEl.GetString() : null;
            string? website = data.TryGetProperty("website", out var webEl) ? webEl.GetString() : null;
            return new AppDetailsResponse(appId, name, shortDescription, detailed, type, isFree, categories, genres, header, website);
        }
    }

    public static class ReviewsResponseFactory
    {
        public static ReviewsResponse FromRaw(int appId, ReviewsRaw raw)
        {
            var list = raw.Reviews.Select(r => new Review(
                int.TryParse(r.RecommendationId, out var rid) ? rid : 0,
                r.Author.SteamId,
                r.Author.PlaytimeAtReview,
                r.Author.PlaytimeForever,
                r.Review,
                r.VotesUp,
                r.VotesFunny,
                r.VotedUp,
                DateTimeOffset.FromUnixTimeSeconds(r.TimestampCreated).UtcDateTime
            )).ToList();
            return new ReviewsResponse(appId, list);
        }
    }
}
