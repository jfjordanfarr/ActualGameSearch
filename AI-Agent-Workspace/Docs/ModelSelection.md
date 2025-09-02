## Model Selection & Integration Plan

Chosen Base Model: OpenCLIP ViT-B/32 (multimodal text+image) – dimension 512.

Rationale:
* 512-d matches existing placeholder dimension → schema unchanged.
* Broad ecosystem adoption; plenty of parity references.
* Multimodal from day one (even if we only embed text initially).

Target Footprint:
* Original FP32 weights ~150MB.
* Int8 quantized ONNX target ~50–60MB.
* Upper bound acceptable (project owner approved up to 300MB) but we aim smaller for faster first paint and mobile viability.

Mobile Considerations:
* Avoid large runtime memory spikes: prefer static quantization over dynamic where feasible.
* Evaluate WASM SIMD availability; fallback path if not present.

Artifacts To Produce:
| Artifact | Purpose | Hash (SHA256) | Notes |
|----------|---------|---------------|-------|
| model.onnx (int8) | Inference graph | (TBD) | Generated via onnxruntime quantization script. |
| tokenizer.json | BPE merges + vocab | (TBD) | Extract / adapt from openclip repo. |
| license.txt | Model license | (TBD) | Must include attribution. |

Manifest Extensions (implemented): `model_file_sha256`, `tokenizer_file_sha256` (nullable until populated).

Server Integration Steps:
1. Add download/quantization script (PowerShell + optional Python fallback) writing to `models/openclip-vitb32/`.
2. Implement `OnnxEmbeddingProvider` using Microsoft.ML.OnnxRuntime.
3. Gate with env flag `USE_DETERMINISTIC_EMBEDDINGS` (default false once model verified) to allow fallback.
4. Regenerate ETL dataset with real embeddings; update manifest with file hashes.
5. Update parity tests (calibration prompts) to new vectors.

Client Integration Steps:
1. Add lightweight JS tokenizer + ONNX Runtime Web loader.
2. Provide Blazor interop for `EmbedAsync(text)` returning float32 array (normalized).
3. Update `/parity` page to show cosine similarity server vs client.
4. Later: add image embedding path (lazy load) and fusion logic.

Open Questions:
* Should we host model assets via CDN/R2 for cache-friendly ETag support vs bundling? (leaning yes for future updates)
* Quantization approach: dynamic vs static (requires calibration set). Evaluate speed difference.
* Tokenizer parity test: include a few canonical sentences with known token id sequences.

Next Actions (implementation order):
1. Add `OnnxEmbeddingProvider` skeleton (no model load yet, just throws if assets missing).
2. Add env + DI wiring.
3. Add hash utility + manifest assignment placeholders.
4. Script scaffolding (download + quantize) – commit without large binaries.
5. Execute script locally (out of repo), confirm hashes, then decide hosting path.

Document version: 2025-09-01