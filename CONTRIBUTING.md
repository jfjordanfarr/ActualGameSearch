# Contributing Guide

## Code of Conduct
Be respectful; follow project maintainers' guidance. (Full policy TBD – PR welcome.)

## Development Workflow
1. Fork & clone.
2. Create a feature branch: `feat/<short-topic>`.
3. Run `dotnet build` and `dotnet test` (all tests must pass).
4. Make changes with minimal diff noise; update docs for contract changes.
5. Submit PR with concise description (what/why, any follow-up tasks).

## Tests
```
 dotnet test
```
Add or extend tests when:
- Changing embedding logic or dimension.
- Altering manifest schema or validation.
- Adjusting search scoring behavior.

## Commit Messages
Conventional style preferred:
- `feat: add onnx loader stub`
- `fix: recompute db hash on startup`
- `docs: getting started guide`

## Design Principles
- Deterministic & reproducible ETL (seeded random where sampling used).
- Explicit manifest fields for any model/storage change.
- Keep runtime hot path lean; heavier transforms happen in ETL.
- Client parity: any server-side embedding change requires updated client assets + parity tests.

## Adding a Model
1. Drop ONNX + tokenizer assets (avoid committing >100MB single files; consider external hosting + download script).
2. Update `EmbeddingModelDefaults` (id, dimension, tokenizer, quantization).
3. Regenerate dataset via ETL; commit new manifest & calibration prompt vectors (or regeneration script output).
4. Update documentation (Model Strategy, Getting Started) with new size + hash.

## Filing Issues
Include:
- Reproduction steps
- Expected vs actual behavior
- Environment (OS, .NET version)

## Security
Never commit secrets (Steam API keys, etc.). If a secret is leaked, rotate immediately.

## License Contributions
By contributing you agree your code is licensed under the project MIT license.

---
Document version: 2025-09-01
