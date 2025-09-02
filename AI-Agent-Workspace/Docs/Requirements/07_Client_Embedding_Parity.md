# 07 – Client Embedding Parity & Verification

## Objective
Guarantee that embeddings generated in-browser (client) are functionally equivalent to offline ETL embeddings so cosine ranking remains stable. This is the SECOND proof pillar.

## Parity Definition
Given a text input T (and later image I):
Let E_offline(T) and E_client(T) be normalized vectors. Accept parity if:
`cos(E_offline, E_client) ≥ 0.995` (bootstrap threshold) for a calibration set.

## Calibration Set
| Type | Size | Source |
|------|------|--------|
| Game titles | 100 | Random sample |
| Short review excerpts | 100 | Filtered reviews |
| Synthetic descriptors | 50 | Curated prompts (‘dark gothic roguelike’, etc.) |

Store calibration inputs in version-controlled json: `embedding_parity_calibration.json`.

## Test Procedure
1. Load model offline; embed all calibration prompts → reference.csv.
2. In Blazor test harness (headless playwright or unit via JS interop), embed same prompts.
3. Compute cosine; produce report.
4. Fail build if any < threshold OR mean < 0.998.

## Drift Monitoring
On model update:
1. Increment model id in manifest.
2. Regenerate offline embeddings & parity refs.
3. Re-run parity; if fail, either raise threshold deviation note or rollback.

## Image Extension (Later)
Add additional threshold: `cos(E_img_only, E_text_desc_of_image) ≥ 0.30` (sanity, not strict) using a small caption set.

## Tooling Outline
| Component | Responsibility |
|-----------|----------------|
| `EmbeddingParity.Reference` (.NET) | CLI to embed calibration prompts offline |
| Blazor `IEmbeddingClient` | Wrapper calling JS ONNX Runtime |
| `ParityTests` | Compares vectors & asserts thresholds |

## Storage Format
Reference vectors in JSON (float32 compressed optional). Example:
```json
{ "model":"mini-clip-int8", "dim":512, "vectors":[ {"id":"title:counter_strike","v":[0.01,...]} ] }
```

## Failure Diagnostics
On failure: log max angle error, first 5 largest deviation prompts.

---
Document version: 2025-09-01
