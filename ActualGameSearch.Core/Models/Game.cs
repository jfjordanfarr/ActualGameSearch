namespace ActualGameSearch.Core.Models;

public record Game(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    bool IsAdult)
{
    public static Game CreateNew(string name, string description, IEnumerable<string>? tags = null, bool isAdult = false)
        => new(
            Guid.NewGuid(),
            name,
            description,
            (tags ?? Enumerable.Empty<string>())
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            isAdult);

    // Hydrate from persisted fields (assumes tags already clean)
    public static Game Hydrate(Guid id, string name, string description, IEnumerable<string> tags, bool isAdult)
        => new(id, name, description, tags.ToList(), isAdult);
}
