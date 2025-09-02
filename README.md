# Actual Game Search

Ultra low-cost hybrid semantic search for video games. Offline ETL builds a reproducible SQLite dataset (`games.db`) with precomputed embeddings and a manifest of integrity hashes; the runtime API + Blazor UI serve fast cosine similarity search. Planned evolution introduces a real (MiniCLIP/MobileCLIP) text embedding model first, then image fusion, then hybrid keyword + vector ranking.

## Highlights
- **Reproducible Dataset**: Deterministic ETL (seeded sampling) emits `games.db` + `manifest.json` (db hash, sample embedding hash, app list hash).
- **Client Embedding Parity (Roadmap)**: Query embeddings will be computed in-browser (ONNX Runtime Web) matching server/ETL model for cost-near-zero search.
- **Observability**: OpenTelemetry traces + metrics (search latency, request counts) via Aspire AppHost.
- **Deterministic Placeholder**: Current deterministic embedding provider ensures stable tests while real model integration is staged.

## Repository Structure
| Path | Purpose |
|------|---------|
| `ActualGameSearch.AppHost/` | .NET Aspire entry (orchestrates API, WebApp, OTel) |
| `ActualGameSearch.Api/` | Minimal API (ingest, search, embedding debug, manifest) |
| `ActualGameSearch.WebApp/` | Blazor Server UI (search + parity pages) |
| `ActualGameSearch.Core/` | Domain, repositories, embedding abstractions, config |
| `ActualGameSearch.ETL/` | Offline dataset builder & embeddings export mode |
| `ActualGameSearch.Tests/` | Unit + integration tests (parity, integrity) |
| `AI-Agent-Workspace/Docs/` | Architecture & strategy documentation |

See `AI-Agent-Workspace/Docs/GettingStarted.md` for detailed setup.

## Quick Start
```
# Build
 dotnet build
# Run ETL (creates games.db + manifest.json)
 dotnet run --project ActualGameSearch.ETL
# Launch Aspire (API + WebApp + OTel)
 dotnet run --project ActualGameSearch.AppHost
```
Open the Aspire dashboard URL shown in console; visit the WebApp `/search` route.

## Environment Variables
Key variables (full table in GettingStarted):
- `ACTUAL_GAME_SEARCH_DB` – override SQLite path.
- `ETL_SAMPLE_SIZE`, `ETL_RANDOM_SEED`, `ETL_FORCE_REFRESH` – ETL controls.
- `ACTUALGAME_STRICT_MANIFEST` – fail startup on manifest / hash mismatch.
- `ACTUALGAME_ETL_ON_START` – dev-only automatic ETL.
- `ACTUALGAME_EXPORT_EMBEDDINGS_TOP` – export mode (write sample embeddings JSON).

## Roadmap (Excerpt)
1. Replace placeholder with MiniCLIP/MobileCLIP text encoder (ONNX int8) for real semantic search.
2. Client-side ONNX embedding & vector POST search endpoint.
3. Review embeddings + weighted fusion.
4. Image embeddings & multimodal fusion.
5. Hybrid keyword (FTS) + vector rank fusion; later nearest-neighbor precomputation.

## Contributing
See `CONTRIBUTING.md`.

## License
MIT (see `LICENSE`).

## Security
No production secrets stored. Do not commit API keys (e.g., `STEAM_API_KEY`). Report issues via GitHub Issues or follow `SECURITY.md` instructions.

---
Document version: 2025-09-01
