using ActualGameSearch.Core.Models;

namespace ActualGameSearch.Core.Services;

public interface IGameRepository
{
    void Add(Game game);
    Game? Get(Guid id);
    IEnumerable<Game> GetAll();
    // Optional: returns precomputed embedding for a game if repository backed by embeddings table, else null.
    float[]? GetEmbedding(Guid id);
}

internal sealed class InMemoryGameRepository : IGameRepository
{
    private readonly Dictionary<Guid, Game> _store = new();
    private readonly object _lock = new();

    public void Add(Game game)
    {
        lock (_lock)
        {
            _store[game.Id] = game;
        }
    }

    public Game? Get(Guid id)
    {
        lock (_lock)
        {
            return _store.TryGetValue(id, out var g) ? g : null;
        }
    }

    public IEnumerable<Game> GetAll()
    {
        lock (_lock)
        {
            // return snapshot to avoid external mutation concerns
            return _store.Values.ToArray();
        }
    }

    public float[]? GetEmbedding(Guid id) => null; // no precomputed embeddings
}

// Basic SQLite-backed implementation (read-mostly). Write path minimal for parity with Add.
public sealed class SqliteGameRepository : IGameRepository, IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, float[]> _embeddings = new();
    private int _embeddingDim = 0;

    public SqliteGameRepository(string dbPath)
    {
        _conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        // Create minimal schema only if absent (allows reuse of richer ETL-created DB)
        var cmd = _conn.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Games (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    Tags TEXT NOT NULL,
    IsAdult INTEGER NOT NULL
);";
        cmd.ExecuteNonQuery();

        // If an Embeddings table (Id TEXT, Vector BLOB) exists, load it eagerly once.
        try
        {
            var check = _conn.CreateCommand();
            check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Embeddings'";
            var exists = check.ExecuteScalar() != null;
            if (exists)
            {
                var dimCmd = _conn.CreateCommand();
                dimCmd.CommandText = "PRAGMA table_info(Embeddings)"; // we assume schema: GameId TEXT PRIMARY KEY, Vector BLOB
                // We can't get dim directly; read first row.
                var sel = _conn.CreateCommand();
                sel.CommandText = "SELECT GameId, Vector FROM Embeddings LIMIT 1";
                using (var rdr = sel.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        var blob = (byte[])rdr[1];
                        _embeddingDim = blob.Length / sizeof(float);
                        // rewind not needed; we'll refill below
                    }
                }
                if (_embeddingDim > 0)
                {
                    var all = _conn.CreateCommand();
                    all.CommandText = "SELECT GameId, Vector FROM Embeddings";
                    using var r2 = all.ExecuteReader();
                    while (r2.Read())
                    {
                        var idStr = r2.GetString(0);
                        var blob = (byte[])r2[1];
                        if (blob.Length != _embeddingDim * sizeof(float)) continue; // skip corrupt
                        var vec = new float[_embeddingDim];
                        System.Buffer.BlockCopy(blob, 0, vec, 0, blob.Length);
                        if (Guid.TryParse(idStr, out var gid)) _embeddings[gid] = vec;
                    }
                }
            }
        }
        catch { /* ignore embeddings load failures for now */ }
    }

    public void Add(Game game)
    {
        lock (_sync)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Games (Id,Name,Description,Tags,IsAdult) VALUES ($id,$n,$d,$t,$a)";
            cmd.Parameters.AddWithValue("$id", game.Id.ToString());
            cmd.Parameters.AddWithValue("$n", game.Name);
            cmd.Parameters.AddWithValue("$d", game.Description);
            cmd.Parameters.AddWithValue("$t", string.Join(',', game.Tags));
            cmd.Parameters.AddWithValue("$a", game.IsAdult ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
    }

    public Game? Get(Guid id)
    {
        lock (_sync)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Description,Tags,IsAdult FROM Games WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return Map(r);
        }
    }

    public IEnumerable<Game> GetAll()
    {
        lock (_sync)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,Description,Tags,IsAdult FROM Games";
            using var r = cmd.ExecuteReader();
            var list = new List<Game>();
            while (r.Read()) list.Add(Map(r));
            return list;
        }
    }

    public float[]? GetEmbedding(Guid id)
    {
        lock (_sync)
        {
            return _embeddings.TryGetValue(id, out var v) ? v : null;
        }
    }

    private static Game Map(Microsoft.Data.Sqlite.SqliteDataReader r)
    {
        var id = Guid.Parse(r.GetString(0));
        var name = r.GetString(1);
        var desc = r.GetString(2);
        var tags = r.GetString(3).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var isAdult = r.GetInt32(4) == 1;
        return Game.Hydrate(id, name, desc, tags, isAdult);
    }

    public void Dispose()
    {
        try { _conn.Dispose(); } catch { }
    }
}
