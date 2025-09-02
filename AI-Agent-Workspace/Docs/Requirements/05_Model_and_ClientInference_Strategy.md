# Embedding & Client Inference Strategy (Addendum)

## Goals
* Single (or minimal) multimodal embedding space for text + images.
* All query-time embeddings computed client-side in the browser to drive cost near zero.
* Server (or static asset bundle) only stores precomputed game embeddings + metadata and executes lightweight similarity + keyword fusion.
* Preserve rich 2023 scoring (resonance, time weighting) while relaxing hard exclusion of adult content: retain data, default filtered, user-toggle to include.

## Candidate Multimodal Models (Browser Feasibility)
| Model | Dim | Size (approx ONNX quantized) | Strengths | Notes |
|-------|-----|------------------------------|----------|-------|
| OpenAI CLIP ViT-B/32 (open source replicas) | 512 | ~150MB float32 (~40–55MB int8) | Mature, many ports | Text+Image unified space. |
| SigLIP (Base Patch16-384) | 768 | ~300MB float32 (~90MB int8) | Strong retrieval performance | Larger; may need lazy chunked download. |
| MiniCLIP / MobileCLIP variants | 512 | 25–60MB int8 | Smaller footprint | Slightly lower semantic quality; better initial UX. |
| BLIP / BLIP2 (vision-language) | >768 | >500MB | Rich captions | Likely too large for initial in-browser embedding-only scenario. |

Initial Recommendation (Phase 1 – Text Only): Adopt an open MiniCLIP / MobileCLIP 512‑dim text+image model but initially utilize ONLY the text encoder path. This lets us ship real semantic search quickly while leaving the image encoder (and image ETL) for a later phase. The dimension (512) matches our current placeholder so stored vectors remain compatible with a regeneration pass.

## Embedding Strategy (Incremental)
Phase 1 (Current Work):
* Replace deterministic placeholder with real MiniCLIP/MobileCLIP text encoder (ONNX int8) – both ETL & client.
* Canonical game vector = Normalize( w_descr * description_vector ) until reviews/image pipelines land. (w_descr = 1.0)

Phase 2 (Reviews):
* Add review sampling + weighting; vector = Normalize( w_reviews * Mean(review_vectors_weighted) ⊕ w_descr * description_vector ).

Phase 3 (Images – Deferred):
* Extend ETL to fetch capsule / hero images, run image encoder; aggregate via weighted mean of topK image embeddings.
* Final formula (original vision) reinstated with weights versioned in manifest.

Client Query Embedding:
* Always produced locally via ONNX Runtime Web; future mixed text+image query uses same weighted fusion pipeline on the client.

## Adult Content Policy
* ETL retains adult / mature games; each game assigned flags: is_adult, is_nsfw (granular later).
* API & client default: exclude flagged unless user opts in (includeAdult=true).
* Manifest documents filtering ruleset version.

## Manifest Extensions
Add fields:
```jsonc
{
  "schema_version": "1.0",
  "embedding_model": {
    "id": "mini-clip-int8",
    "dimension": 512,
    "quantization": "int8",
    "preproc_version": "clip-tokenizer-v1"
  },
  "fusion_weights": { "text": 0.5, "description": 0.2, "images": 0.3 },
  "adult_filter_default": true,
  "resonance_formula": "time_weighted_resonance = resonance_score / log_base_365(age_days)"
}
```

## Search Flow (Target Hybrid w/ Client Embeddings)
1. Client generates embedding locally.
2. Client sends vector + filters (or, in fully offline mode, queries local SQLite).
3. Server performs:
   * (Optional) Keyword/FTS stage → candidate ids.
   * Vector similarity (cosine) among candidate (or whole set early on).
   * Apply adult filter (unless includeAdult=true).
   * Return ranked results (id, score, snippet fields).

## Local Dev & Future Offline Mode
* Provide a bundled SQLite (game_meta.db) containing tables: games, game_embeddings (rowid=game_id, vector BLOB), nearest_neighbors.
* For offline exploration: run entire hybrid search in browser using WebAssembly SQLite + JS/WASM vector ops (later phase).

## Open Questions / Next Decisions
* Publish location for ONNX + tokenizer (R2 bucket vs static wwwroot bundle, considering caching headers & incremental updates).
* Quantization pipeline reproducibility script (document exact commit + calibration dataset).
* Tokenizer implementation (pure JS vs WASM) – evaluate performance & bundle size.
* Progressive image model enablement (lazy download only when user toggles "Include image semantics").

## Immediate Engineering Tasks
1. Introduce domain flag (IsAdult) – done.
2. Extend API search endpoint with includeAdult toggle – done.
3. Scaffold manifest POCO & placeholder loader (next step).
4. Prepare client component for future embedded search UI.
 5. Document vector storage layout (see `VectorFormat.md`) – done.
 6. Add strict manifest validation + on-start ETL flag (`ACTUALGAME_ETL_ON_START`) – implemented (dev only; production Cloudflare container triggers ETL, flag remains OFF in prod).

## Production Topology Note
`ACTUALGAME_ETL_ON_START` is a developer convenience inside the Aspire harness. In the Cloudflare-first production design the ETL runs via Container orchestrated by a Worker/Durable Object scheduler; the API surface should treat the dataset as immutable for the lifecycle of a deployment. The flag MUST NOT be enabled in production to avoid surprise dataset churn.

## Vector Format Reference
See `VectorFormat.md` for authoritative BLOB layout (float32 little-endian, contiguous, L2-normalized). Any change to layout requires incrementing a future `vector_layout_version` manifest field.

---
Document version: 2025-09-01 (rev1 text-first focus)
