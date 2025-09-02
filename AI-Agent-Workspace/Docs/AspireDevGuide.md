# Aspire Development Guide

This guide explains how the .NET Aspire AppHost composes the Actual Game Search services for local development & observability.

## Components
| Resource | Description |
|----------|-------------|
| API (ActualGameSearch.Api) | Minimal API providing ingest, search, embedding debug, manifest endpoints |
| WebApp (ActualGameSearch.WebApp) | Blazor Server UI for search & embedding parity |
| OpenTelemetry Collector | Receives traces/metrics/logs from services |

## Launching
```
 dotnet run --project ActualGameSearch.AppHost
```
The console prints the Aspire dashboard URL (e.g. http://localhost:****). Open it to view:
* Live service health
* Traces (search request spans)
* Metrics (request count, latency histogram)

## Observability
The API emits:
* ActivitySource: `ActualGameSearch.Core` around search operations
* Metrics:
  * `game_search.requests` (counter)
  * `game_search.latency.ms` (histogram)

Set `OTEL_CONSOLE_EXPORTER=1` to also dump spans to console during development.

## Data Flow (Current Text-Only Phase)
1. WebApp user enters query → server invokes `IGameSearchService` (placeholder embedding computed server-side).
2. Search service generates query embedding (placeholder) & computes cosine similarity against cached embeddings (or recomputed if using in-memory repository).
3. Results rendered in `/search` table.
4. `/parity` page calls API debug embedding endpoint and compares to locally generated embedding for transparency.

## Planned Client-Side Embedding
Once ONNX model integrated:
* WebApp loads tokenizer + ONNX model (lazy) and computes query embedding locally.
* Query vector either:
  * Sent to API: POST `/api/games/search/vector` { vector: [...], take } (to be added), OR
  * Used fully client-side once we have a browser SQLite/vector wasm path.
* Telemetry updated to record client-reported latency segments.

## Startup ETL (Optional)
`ACTUALGAME_ETL_ON_START=true` causes API to run ETL during startup if no `games.db` exists. Recommended only for quick demos; normal flow is explicit ETL run before launching AppHost.

## Integrity Checks
Startup hosted service validates:
* Embedding model id & dimension
* Recomputed DB SHA256 vs manifest
Warnings (or exceptions if `ACTUALGAME_STRICT_MANIFEST=true`).

## Extending The Graph
Add more services (e.g., vector similarity microservice, or separate review ingestion) by editing the AppHost project to register new resources and linking environment variables.

---
Document version: 2025-09-01
