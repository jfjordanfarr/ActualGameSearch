# ActualGameSearch ETL

Prototype ETL that produces `games.db` (SQLite) plus a `manifest.json` describing reproducibility hashes.

## What it does
1. (Optionally) Loads cached full Steam app list from `apps-cache.json` in the ETL working directory.
2. If cache missing or `ETL_FORCE_REFRESH=1`, fetches fresh list from Steam and rewrites cache.
3. Randomly samples N apps (configurable) with non-empty names.
4. For each sampled app:
   - Fetches app details
   - Fetches a small recent reviews slice (up to 10) for English
   - Writes raw JSON snapshots into `raw-steam/` (`<appId>_details.json`, `<appId>_reviews.json` when present)
   - Constructs a combined description + truncated review text
   - Generates a deterministic 512-d embedding (placeholder) and stores it alongside game row.
5. Computes hashes:
   - `DbSha256` of the final SQLite file
   - `EmbeddingsSampleSha256` over first 5 embedding blobs
   - `AppListSha256` over sorted `appid|name` lines for the full list
6. Writes `manifest.json` with counts & hashes.

## Environment Variables
- `ETL_SAMPLE_SIZE` (int, optional): Target number of sampled apps (default 10, capped at 500). Example PowerShell usage:
  `$env:ETL_SAMPLE_SIZE='50'; dotnet run --project .\ActualGameSearch.ETL\ActualGameSearch.ETL.csproj`
- `ETL_FORCE_REFRESH` ("1" to force): Ignore cache and re-download full app list.
  `$env:ETL_FORCE_REFRESH='1'; dotnet run --project .\ActualGameSearch.ETL\ActualGameSearch.ETL.csproj`
- `STEAM_API_KEY` (optional, currently not required for endpoints in use but reserved for future calls).

## Caching
- Full app list persisted to `apps-cache.json` (raw list of `appid` + `name`).
- To refresh: set `ETL_FORCE_REFRESH=1`.

## Outputs
- `games.db` (tables: Games, Embeddings)
- `manifest.json` (hash fields + sample metadata)
- `raw-steam/` JSON snapshots (for sampled apps)

## Determinism Notes
Sampling is random each run; reproducibility is not yet controlled via seed. Future enhancement could add `ETL_RANDOM_SEED` to lock sampling order and strengthen manifest assurances.

## Next Enhancements (planned)
- Configurable reviews fetch count
- Optional deterministic sampling seed
- Switch placeholder embedding to compact ONNX model behind a feature flag
- Incremental ETL (append new games) & drift detection
