namespace ActualGameSearch.Core.Manifest;

public sealed record EmbeddingModelInfo(string Id, int Dimension, string Tokenizer, string Quantization);
public sealed record DatasetManifest(
    string SchemaVersion,
    DateTime GeneratedUtc,
    EmbeddingModelInfo Model,
    double AlphaReviews,
    double BetaDescription,
    int GameCount,
    int TotalReviewsUsed,
    int MinReviewUniqueWords,
    int MinReviewsPerGame,
    bool AdultIncluded,
    string DbSha256,
    string EmbeddingsSampleSha256,
    string AppListSha256,
    int SampledAppIds,
    int? SampleSeed)
{
    public static DatasetManifest Placeholder() => new(
        "1.0", DateTime.UtcNow,
        new EmbeddingModelInfo(Core.Model.EmbeddingModelDefaults.ModelId, Core.Model.EmbeddingModelDefaults.Dimension, Core.Model.EmbeddingModelDefaults.TokenizerId, Core.Model.EmbeddingModelDefaults.Quantization),
        1.0, 0.0, 0, 0, 20, 20, true, string.Empty, string.Empty, string.Empty, 0, null);
}
