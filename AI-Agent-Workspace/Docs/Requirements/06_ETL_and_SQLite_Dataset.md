# 06 – ETL & SQLite Dataset Specification

## Objective
Produce a reproducible, versioned SQLite dataset (games.db) containing game metadata, unified multimodal embeddings, and nearest-neighbor precomputations suitable for ultra‑low‑cost hybrid semantic search. This is the FIRST proof pillar.

## Scope (MVP)
1. Ingest a small but representative slice (e.g., 1–5K games) to validate pipeline & hosting footprint.
2. Generate one embedding vector per game from text sources only (reviews + descriptions) – image fusion optional in v0.
3. Store results in SQLite with deterministic ordering & a manifest.json.
4. Provide validation script that: (a) counts rows, (b) asserts dimension, (c) samples cosine similarity stability.

## Data Inputs
| Name | Source | Use | Notes |
|------|--------|-----|-------|
| Raw Metadata | Steam storefront API (appdetails) | Title, descriptions, header image URL | Cached to disk/json.
| Reviews | Steam appreviews endpoint | Text corpus + resonance weighting | Filter rules below.
| Images (later) | Storefront screenshot URLs | Image embeddings | Defer until text path solid.

## Filtering (v0)
| Rule | Purpose | Default |
|------|---------|---------|
| type == 'game' | Exclude DLC, soundtracks | ON |
| released == true | Avoid unreleased | ON |
| min review length (unique words ≥ 20) | Quality | ON |
| min qualifying reviews per game ≥ 20 | Stability | ON |
| adult content | Keep | ON (flag only) |

## Embedding Model (v0)
| Field | Value |
|-------|-------|
| id | mini-clip-int8 (placeholder) |
| dimension | 512 |
| quantization | int8 |
| tokenizer | clip-tokenizer-v1 |

Switching models requires regenerating ALL embeddings (no legacy preservation policy).

## Aggregation (Game Vector)
For each game:
1. Select top N reviews (e.g., N=100) by time_weighted_resonance (later tunable) OR just first 100 after filtering (bootstrap mode).
2. Embed each review → r_i.
3. Weighted mean: R = Normalize( Σ w_i * r_i ), where w_i = 1 (bootstrap) → later time_weighted_resonance.
4. Description embedding D (optional early skip). If present: G = Normalize( αR + βD ), defaults α=1, β=0 initially.
5. Store G.

## SQLite Schema (v0)
```sql
CREATE TABLE games (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  steam_appid INTEGER NOT NULL UNIQUE,
  title TEXT NOT NULL,
  description TEXT,
  review_count INTEGER NOT NULL,
  is_adult INTEGER NOT NULL DEFAULT 0
);

-- Raw embedding bytes (float32 or int8 depending on strategy). Keep a parallel meta table for dimension.
CREATE TABLE game_embeddings (
  game_id INTEGER PRIMARY KEY REFERENCES games(id) ON DELETE CASCADE,
  vector BLOB NOT NULL
);

-- Optional precomputed neighbors (early can leave empty)
CREATE TABLE nearest_neighbors (
  game_id INTEGER NOT NULL,
  neighbor_game_id INTEGER NOT NULL,
  rank INTEGER NOT NULL,
  score REAL NOT NULL,
  PRIMARY KEY (game_id, neighbor_game_id)
);

CREATE INDEX idx_neighbors_game ON nearest_neighbors(game_id);
```

## Manifest (manifest.json)
```jsonc
{
  "schema_version": "1.0",
  "generated_utc": "2025-09-01T00:00:00Z",
  "model": { "id": "mini-clip-int8", "dim": 512, "tokenizer": "clip-tokenizer-v1" },
  "aggregation": { "alpha_reviews": 1.0, "beta_description": 0.0 },
  "counts": { "games": 0, "total_reviews_used": 0 },
  "filters": { "min_review_unique_words": 20, "min_reviews_per_game": 20 },
  "flags": { "adult_included": true },
  "hashes": { "db_sha256": "", "embeddings_sample_sha256": "" }
}
```

## Pipeline Phases
1. Discover appids.
2. Fetch + cache metadata.
3. Fetch + filter reviews.
4. (Bootstrap) Embed using CLI / local ONNX runner.
5. Aggregate vectors per game.
6. Write SQLite.
7. Compute neighbors (optional): brute-force cosine for first version over small corpus.
8. Emit manifest + integrity hashes.

## Validation Steps
| Check | Method | Pass Criteria |
|-------|--------|---------------|
| Row count match | COUNT(*) across tables | games == embeddings rows |
| Vector dimension | Decode 3 sample rows | = manifest.model.dim |
| Cosine self | cos(g,g)=1 numeric tolerance | ≥0.999 float |
| Hash reproducibility | Re-run pipeline subset | Stable hash |

## Deliverables (v0)
* `games.db`
* `manifest.json`
* `validate.py` (or .NET tool) – prints PASS/FAIL summary.

## Exit Criteria for Proof #1
* Build < 10 min on dev machine for 1K game slice.
* SQLite file < ~50MB for slice.
* Validation script passes.

---
Document version: 2025-09-01
