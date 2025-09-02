# Getting Started

This guide walks a new contributor through running the Actual Game Search stack locally using .NET Aspire, generating (or loading) the dataset, and performing a semantic search end‑to‑end.

## Prerequisites
* .NET 10 (preview) SDK installed
* (Optional) Steam API key in `STEAM_API_KEY` environment variable to enrich ETL (otherwise falls back to minimal placeholder data)
* Windows / macOS / Linux (x64 / Arm64)

## Repository Layout (High Level)
| Path | Purpose |
|------|---------|
| `ActualGameSearch.AppHost/` | Aspire orchestration entrypoint (starts API + WebApp + OTel) |
| `ActualGameSearch.Api/` | Minimal API (ingest, search, debug embedding, manifest) |
| `ActualGameSearch.WebApp/` | Blazor Server UI (search + parity pages) |
| `ActualGameSearch.Core/` | Domain models, repositories, embedding abstractions |
| `ActualGameSearch.ETL/` | Offline dataset builder (games.db + manifest.json) |
| `ActualGameSearch.Tests/` | Unit & integration tests (parity, integrity) |
| `AI-Agent-Workspace/Docs/` | Architecture & requirements docs |

## Quick Start (Happy Path)
1. Build solution:
```
 dotnet build
```
2. Run ETL to create `games.db` + `manifest.json` in the build output directory (API & WebApp look here automatically):
```
 dotnet run --project ActualGameSearch.ETL
```
   * Set `ETL_SAMPLE_SIZE` (default 10) and `ETL_RANDOM_SEED` for deterministic sampling.
3. Launch Aspire AppHost (spins up API + WebApp + OTel collector):
```
 dotnet run --project ActualGameSearch.AppHost
```
4. Open the Aspire dashboard (console logs show URL) – verify API & WebApp are healthy.
5. Navigate to the Blazor Web UI (typically `https://localhost:*****`).
6. Open `/search` and type a query; similarity search runs server-side using precomputed embeddings.
7. Open `/parity` to compare local deterministic embedding vs server debug embedding (will evolve to real model parity).

### One-Liner Dev Flow (PowerShell)
```
dotnet build; dotnet run --project ActualGameSearch.ETL; dotnet run --project ActualGameSearch.AppHost
```
Then open the WebApp URL from console and navigate to `/search`.

## Environment Variables
| Variable | Purpose | Default |
|----------|---------|---------|
| `ACTUAL_GAME_SEARCH_DB` | Override path to SQLite dataset | `<base>/games.db` |
| `ACTUALGAME_STRICT_MANIFEST` | Fail startup if manifest/model/db hash mismatch | (off) |
| `ACTUALGAME_ETL_ON_START` | Run ETL automatically when API starts (dev only) | (off) |
| `ETL_SAMPLE_SIZE` | Number of Steam apps sampled during ETL | 10 |
| `ETL_RANDOM_SEED` | Seed for deterministic sampling | (none) |
| `ETL_FORCE_REFRESH` | Ignore cached app list | 0 |
| `ACTUALGAME_EXPORT_EMBEDDINGS_TOP` | If set (int), run export mode (ETL project) instead of full ETL | (unset) |
| `ACTUALGAME_EXPORT_EMBEDDINGS_PATH` | Output path for embeddings export JSON | `embeddings-sample.json` |

## Dataset Artifacts
After ETL:
* `games.db` – Tables: `Games`, `Embeddings`
* `manifest.json` – Model + integrity hashes (db SHA256, embeddings sample, app list hash)
* Optional: `embeddings-sample.json` (export tool)

## Integrity & Validation
* On API startup a hosted service validates model id/dimension and recomputes DB hash (warning or exception if strict mode).
* Tests include parity checks and unit norm validation (`StoredEmbeddings_IfPresent_AreUnitNorm`).

## Switching To A Real Embedding Model
Target model: OpenCLIP ViT-B/32 (text path first, 512‑dim). Transition steps:
1. Run model download script:
```
pwsh ./scripts/download-model-openclip.ps1 -ModelUrl <onnx_url> -TokenizerVocabUrl <vocab_url> -TokenizerMergesUrl <merges_url>
```
2. Set env vars (or put in user profile): `USE_ONNX_EMBEDDINGS=true`, `ACTUALGAME_MODEL_PATH`, `ACTUALGAME_TOKENIZER_VOCAB`, optionally `ACTUALGAME_TOKENIZER_MERGES`.
3. Run ETL again (now produces semantic embeddings + fills `ModelFileSha256`/`TokenizerFileSha256` in manifest).
4. Generate calibration vectors (future step) and commit updated `embedding_parity_calibration.json` with reference embeddings.
5. Add client ONNX Runtime Web + tokenizer JS; compute query embedding locally and POST to `/api/games/search/vector`.
6. (Later) Add image encoder path and fused search.

## Future Image Integration (Deferred)
Image embeddings (capsule / hero images) will be added in a later phase:
* Extend ETL: download small image set, run image encoder of multimodal model.
* Update fusion formula and manifest (store weights and counts).
* Client supports mixed queries (text + image) via weighted vector fusion before search.

## Troubleshooting
| Symptom | Cause | Fix |
|---------|-------|-----|
| Empty search results | No games ingested or ETL not run | Run ETL or ingest manually via API |
| Manifest mismatch warning | Dimension/model changed | Regenerate dataset or enable strict to fail fast |
| Hash mismatch warning | Out-of-sync `games.db` vs `manifest.json` | Re-run ETL to produce matching pair |

## Exporting Sample Embeddings
```
 set ACTUALGAME_EXPORT_EMBEDDINGS_TOP=15
 dotnet run --project ActualGameSearch.ETL
```
Creates `embeddings-sample.json` with first 15 vectors.

## Contributing
1. Fork / branch.
2. Add tests for new behavior (parity, integrity, or search correctness).
3. Run `dotnet test` (all green) before PR.
4. Update relevant docs (this file, strategy docs) when changing data/model contracts.

---
Document version: 2025-09-01
