# Vector Format & Embeddings Storage

Purpose: Canonical reference for how game embeddings are stored in the local SQLite dataset (dev / Aspire harness) so future migration to Cloudflare D1 / Vectorize preserves semantics.

## Table Schema (SQLite Dev Harness)

```
CREATE TABLE Embeddings (
    GameId TEXT PRIMARY KEY REFERENCES Games(Id),
    Vector BLOB NOT NULL -- little-endian float32[DIM]
);
```

- `GameId`: string GUID (same as `Games.Id`).
- `Vector`: Raw binary blob representing a contiguous array of `Dimension` 32-bit IEEE 754 floats (little-endian). No header, no length prefix.

## Dimension Contract
- Current placeholder dimension: `EmbeddingModelDefaults.Dimension` (512).
- Future migration: if dimension changes, a new ETL run MUST regenerate the entire table; mixed-dimension tables are unsupported.

## Serialization
- In C#: allocate `byte[dim * sizeof(float)]` and `Buffer.BlockCopy(float[], 0, byte[], 0, bytes.Length)`.
- Deserialization: allocate `float[dim]`, `Buffer.BlockCopy(blob, 0, float[], 0, blob.Length)`.
- Assumes host endianness = little-endian (true for all target runtime environments). If running on a big-endian platform in the future, explicit byte swap would be required.

## Normalization
- Vectors produced by the embedding provider MUST be L2-normalized before storage (current deterministic provider already normalizes). The search code treats stored vectors as already normalized (cosine reduces to dot product).

## Integrity Signals
- `manifest.json` contains `db_sha256` (hash of whole DB file) and `embeddings_sample_sha256` (hash of small sample of first N vectors). These detect corruption or drift.
- Additional future field: `vector_layout_version` (increment only if byte layout changes — not needed for mere dimension change).

## Rationale For Raw Float32 BLOB
- Fast memory mapping or block copy into managed arrays.
- Avoids JSON encoding overhead / precision loss.
- Compatible with eventual SIMD or GPU pathways (no intermediate parsing).

## Migration Path to Cloudflare
- Local SQLite harness mirrors the eventual D1 + Vectorize split:
  - D1 will own structured & FTS data.
  - Vectorize will hold embeddings; during transition, we keep SQLite BLOBs as the single source of truth for reproducibility.
- Export job: read each `(GameId, Vector)` row, push to Vectorize index (id = GameId, values = parsed float[]).

## Validation Checklist
| Check | Method | Frequency |
|-------|--------|-----------|
| Dimension matches `EmbeddingModelDefaults.Dimension` | Startup manifest validation | Every run |
| BLOB length = dim * 4 | Load-time guard | Every load |
| L2 norm ~1.0 | Spot-check test (<=1e-3 tolerance) | CI (sample) |
| Sample hash stable | Compare `EmbeddingsSampleSha256` | Post-ETL |

## Example Code Snippet (Load)
```csharp
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT GameId, Vector FROM Embeddings";
using var r = cmd.ExecuteReader();
while (r.Read())
{
    var id = Guid.Parse(r.GetString(0));
    var blob = (byte[])r[1];
    if (blob.Length != dim * 4) continue; // skip
    var vec = new float[dim];
    Buffer.BlockCopy(blob, 0, vec, 0, blob.Length);
}
```

## Open Questions
- Will we introduce per-vector quantization (e.g., int8) before upload to Vectorize? (If yes, add `quantization` field + potential dual storage.)
- Need explicit `vector_layout_version`? (Add when first non-trivial change occurs.)

Document version: 2025-09-01
