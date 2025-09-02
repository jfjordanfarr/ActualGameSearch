# 08 – Assistant Rehydration & Continuity Guide

## Purpose
Enable lossless context restoration after chat summarization cycles. This guide is the canonical anchor; all summaries should point here.

## Core Mission (Concise)
Build ultra‑low‑cost hybrid semantic search for games proving:
1. Reproducible ETL → SQLite dataset with unified embeddings.
2. Client-side generation of identical embeddings enabling search parity.

## Current Decisions (Authoritative)
| Area | Decision | Date |
|------|----------|------|
| Embedding model (bootstrap) | mini-clip-int8 (512 dim) | 2025-09-01 |
| Adult content | Retain + flag; filtering UX later | 2025-09-01 |
| Aggregation weights | Reviews only (α=1, β=0) | 2025-09-01 |
| Storage | SQLite (games.db) + manifest.json | 2025-09-01 |
| Parity threshold | 0.995 cosine per prompt | 2025-09-01 |
| Bootstrap dataset size | ~1,000 games (random stratified) | 2025-09-01 |
| Model download budget | ≤ ~90MB (text-first; image later) | 2025-09-01 |
| NN timing | Precompute deferred to P2 | 2025-09-01 |
| Latency posture | Quality > speed (allow slower brute force) | 2025-09-01 |
| Licensing stance | Prefer open-source (non-commercial ok) | 2025-09-01 |
| Priority ordering | (1) Real model parity (2) Hybrid FTS scaffold (3) Neighbor graph (4) Optional direct SQLite in API (NO SQLite in WebApp) | 2025-09-01 |
| UI model load UX | Allow blocking shell | 2025-09-01 |
| Calibration seed size | 20 prompts (expand later) | 2025-09-01 |
| ETL cadence (early) | Manual ad-hoc until P2 | 2025-09-01 |

## Phase Roadmap
| Phase | Goal | Exit Criterion |
|-------|------|----------------|
| P0 | ETL slice + SQLite + manifest | Validation script PASS |
| P1 | Blazor client text embedding parity | Parity tests PASS |
| P2 | Brute-force vector search API (SQLite blob) | Latency < 150ms 1K set |
| P3 | Image embeddings & fusion | Parity extension PASS |
| P4 | Hybrid keyword + vector (FTS + cosine) | Rank fusion tested |
| P5 | Offline batch analytics & visualization artifacts | Precomputed clustering + 2D/3D coords published |

## Canonical Artifacts
| Artifact | Location | Purpose |
|----------|----------|---------|
| `06_ETL_and_SQLite_Dataset.md` | Requirements | ETL contract |
| `07_Client_Embedding_Parity.md` | Requirements | Parity tests |
| `05_Model_and_ClientInference_Strategy.md` | Requirements | Model strategy |
| `games.db` | (generated) | Data store (central: AppConfig.DbPath) |
| `manifest.json` | alongside DB | Version & integrity |
| `embedding_parity_calibration.json` | data folder | Parity inputs |

## Minimal Context Snippet (For Summaries)
"Project builds a cost-near-zero hybrid search: offline (server/home rig) pipeline produces SQLite (games + 512-dim multimodal-ready embeddings, manifest). Client Blazor app computes query embeddings locally with the same model; parity enforced (cos ≥0.995). Adult content retained but simply flagged. Roadmap phases P0–P5 (P5 = offline batch clustering + dimensionality reduction published as static artifacts, NOT full DB download to browser)."

## Summary Regeneration Checklist
1. Confirm decisions table up to date.
2. If introducing new model → update decisions + manifest schema.
3. If changing thresholds → update parity doc.
4. Append new open questions below.

## Open Questions Log
| Date | Question | Status |
|------|----------|--------|
| 2025-09-01 | Which exact MiniCLIP variant (size vs quality)? | Pending selection |
| 2025-09-01 | Tokenizer implementation path (JS vs WASM)? | Pending |
| 2025-09-01 | Which dimensionality reduction method first (TriMAP vs PaCMAP) for P5 artifact? | Pending |

## Assistant Behavioral Reminders
* Prefer concrete PR-style changes over abstract commentary.
* Re-assert parity + ETL goals briefly every major planning turn.
* Do not optimize adult filtering until P2 complete.

## Architecture Progress Map (Current Position)

```mermaid
flowchart LR
	subgraph Offline_Pipeline [Offline / ETL Side]
		A1[Discover & Fetch Raw Data\n(Steam APIs)] --> A2[Filter & Clean Reviews]
		A2 --> A3[Embed Text (Model: mini-clip-int8 placeholder)]
		A3 --> A4[Aggregate Per Game Vector]
		A4 --> A5[Write SQLite: games & embeddings]
		A5 --> A6[Compute DB SHA256 Hash]
		A6 --> A7[Emit manifest.json\n(model + counts + hashes)]
		A7 --> A8[(Validate Dataset)]
	end

	subgraph Proof_Pillars
		P0[P0: Reproducible ETL + SQLite + Manifest]
		P1[P1: Client Embedding Parity]
		P2[P2: Brute-force Vector API]
		P3[P3: Image Fusion]
		P4[P4: Hybrid FTS + Vector]
		P5[P5: Offline Analytics Artifacts]
	end

	subgraph Runtime [Runtime / Blazor]
		R1[Blazor UI Query] --> R2[Client Embedding (future ONNX)] --> R3[Send Vector or Do Local Compare]
		R3 --> R4[Rank & Display Results]
	end

	A7 -->|Manifest Guides| P1
	P0 --> P1 --> P2 --> P4 --> P5
	P2 --> P3

	classDef done fill=#38b000,stroke=#1b4332,color=#fff
	classDef partial fill=#ffb703,stroke=#9a6700,color=#000
	classDef future fill=#adb5bd,stroke=#495057,color=#000

	class A5,A6,A7,P0 done
	class A3,A4 partial
	class P1,P2,P3,P4,P5,R2 future
```

### Where We Are
Current implemented pieces (2025-09-01 snapshot):
1. Deterministic placeholder embedding provider (512-dim) enabling parity scaffolding.
2. API endpoints: ingest, get, search, canonical-text, debug embedding, manifest (if present), health.
3. Search service instrumented with tracing (ActivitySource) + metrics (counter & latency histogram).
4. Dual repository strategy: in-memory fallback; auto-switch to SQLite if `games.db` exists (or env `ACTUAL_GAME_SEARCH_DB`); path resolution centralized via `AppConfig.DbPath`.
5. SQLite repository (games table) + minimal schema auto-create; embeddings table not yet formalized in code (reserved for ETL output integration).
6. Manifest concept documented; runtime endpoint serves `manifest.json` if co-located (hash fields planned, not fully generated yet in current code path).
7. Aspire AppHost orchestrating API, WebApp, and local OpenTelemetry Collector container (OTLP env wiring).
8. Centralized OpenTelemetry setup via `ServiceDefaults` (traces, metrics, logs) with OTLP exporter conditional.
9. Integration & unit tests validating deterministic embedding parity and canonical text pipeline.
10. Health endpoint (`/health`) returning liveness + last search latency.
11. Package management centralized; OTel instrumentation bumped to 1.9.0 (vulnerability addressed).
12. Documentation set: ETL, parity, model strategy, rehydration guide (this), plus added telemetry & model decisions.

Not yet implemented / intentionally deferred:
1. Real Steam data ingestion (no Steam API key usage; no review/game description scraping yet).
2. Actual ETL pipeline producing a persisted embeddings table & full manifest with hashes (current DB bootstrap is ad hoc if present).
3. Real embedding model (MiniCLIP / multimodal candidate) download & inference path (client + ETL) – placeholder only.
4. Image embeddings & fusion (P3).
5. Hybrid keyword + vector rank fusion (P4) – only brute-force cosine on composed text.
6. Nearest neighbor precomputation / graph artifacts (P2/P4).
7. Calibration prompt file with real model reference vectors (will regenerate once real model integrated).
8. EmbeddingsSampleSha256 and NeighborsHash manifest fields.
9. Automated nightly ETL orchestration (manual for now).
10. Review filtering, resonance scoring, aggregation formula deployment.
11. Client-side model loading UX (progress, caching) – not built; only conceptual.
12. Performance optimization (SIMD / batching) – brute force single-thread currently.

Immediate pivot: P0 realism largely achieved (placeholder ETL + embeddings table + manifest). Focus now: hard validation & real model readiness.

### Why the DB SHA256 Hash Matters
Purpose | Value
--- | ---
Integrity Verification | Detects silent corruption / partial uploads of `games.db` (startup service now recomputes hash vs manifest).
Reproducibility Stamp | Lets us assert "same dataset" across runs; hash mismatch = ETL drift (input data or code) → triggers parity recalculation.
Manifest Contract Anchor | Clients / deployment scripts can quickly decide if they must invalidate caches or rebuild derived artifacts (neighbors, PCA/UMAP coords later).
Security / Supply Chain | Prevents tampered copies being served (hash is published + optionally signed later).
Test Gating | Future CI step: recompute hash after ETL; must match unless intentionally bumped (change log entry required).
Multi‑Artifact Consistency | When we add `EmbeddingsSampleSha256`, we can cheaply prove that vector byte ordering + normalization are unchanged without hashing the entire DB again.

### Relationship to Parity Work
Parity tests will pin against: (model id, model dimension, db hash). If any element changes: recompute offline reference vectors & re-run threshold checks. The hash closes the loop so we do not accidentally mix new vectors with old manifest metadata.

### Light vs Full Hashing Later
We may later introduce:
* `EmbeddingsSampleSha256`: hash of concatenated first N (e.g., 50) vectors → quick drift sentinel.
* `NeighborsHash`: when precomputed nearest neighbors land (P2/P4), allows verifying graph artifact alignment with embeddings.

## Pre-Real-Embedding Implementation Checklist
Goal: be ready to swap in real multimodal model with minimal refactor.

P0 Hardening (status):
1. DONE: ETL skeleton (`EtlRunner`) + console + optional API flag `ACTUALGAME_ETL_ON_START`.
2. DONE: Manifest fields (model id/dim, counts, db/app list/sample hashes).
3. DONE: Repository uses embeddings table if present (precomputed vectors loaded once).
4. DONE: Calibration prompts file present (vectors pending real model integration).
5. DONE: Hash validation test (conditional) in test suite.
6. PARTIAL: Config centralization (model constants done; unify DB path/env next).
Strict flags: `ACTUALGAME_STRICT_MANIFEST` (fail fast), `ACTUALGAME_ETL_ON_START` (dev convenience only; disable in prod Cloudflare container/Worker orchestration).

P1 Preparation (before real model drop):
7. Embedding Provider Abstraction: Introduce interface for async embedding (anticipate WASM/ONNX async). Keep sync adapter for placeholder.
8. Model Metadata Enforcement: On startup validate manifest model_dim matches provider dimension.
9. Client Parity Test Harness: Expand integration tests to load calibration prompts from JSON and compare server vs local provider once real model lands.
10. Vector Format Decision: Decide storage layout (row-per-vector, BLOB concatenation, or separate table). Start with row-per-vector (game_id TEXT, vector BLOB) for clarity.
11. Serialization Contract: Define that each embedding is stored as 512 float32 little-endian contiguous bytes (document).

Real Model Integration (switch point):
12. Introduce ONNX model artifact (server + client). Provide loader stub returning dimension & version hash.
13. Replace placeholder provider server-side first; run ETL; generate fresh calibration set vectors offline; commit reference file.
14. Add client WASM inference (initial blocking load acceptable) and parity tests.

Post-Integration (staging for P2):
15. Optimize search (SIMD / memory map embeddings) once correctness proven.
16. Add optional `LIMIT` prefilter by keyword (full-text virtual table) then vector re-rank.
17. Begin nearest neighbor precomputation script (deferred until embeddings stable).

## Updated Inputs Wanted (If Any)
You can refine: target first ETL game count, acceptable model size, SIMD urgency threshold, or calibration prompt themes (genres vs mechanics). Defaults: 1K games, ≤90MB model, SIMD not urgent until >5K games, prompts balanced across genres.

## Immediate Next (Aligned With Provided Inputs)
1. Implement ETL skeleton & manifest emission (placeholder embeddings) (P0 completion).
2. Add calibration prompts JSON (texts only) & test loader.
3. Introduce async-capable embedding provider interface + adapter.
4. Add embedding table + row-per-vector storage + manifest dimension validation.
5. Create hash validation CLI/test.
6. Prepare ONNX loader stub (returning model id, dimension) ahead of real artifact.
7. Extend parity integration test to iterate calibration prompts (currently single ingest scenario).
8. Documentation: Update manifest spec doc with finalized field names & storage contract.

NOTE: Removed prior step suggesting direct WebApp SQLite load to honor API-centric boundary.

## Adding New Decisions
When a decision is made: update Decisions table + add row to change log (append below).

## Change Log
| Date | Change | Rationale |
|------|--------|-----------|
| 2025-09-01 | Initial guide created | Persistence across chat windows |
| 2025-09-01 | Clarified Phase P5 meaning (batch analytics, not client offline DB) | Prevent misinterpretation of "offline" |
| 2025-09-01 | Added dataset size, model budget, priorities, ETL cadence | Incorporated user guidance |
| 2025-09-01 | Adjusted next steps to API-centric (removed direct WebApp DB load) | Respect architectural boundary |

---
Document version: 2025-09-01
