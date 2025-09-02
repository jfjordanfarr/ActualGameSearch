namespace ActualGameSearch.Core.Services;

/// <summary>
/// Central place that defines how a game's textual fields are concatenated for embedding.
/// Ensures server ETL, API, and client parity code can share exact logic.
/// Change here => parity recalibration required.
/// </summary>
public static class GameTextComposer
{
    public static string Compose(string name, string description, IEnumerable<string> tags)
        => string.Join(' ', new[]{ name, description, string.Join(' ', tags)});
}