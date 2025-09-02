# Security Policy

## Supported Versions
Active development happens on `main`. No formal LTS yet.

## Reporting a Vulnerability
Open a GitHub Security Advisory or email the maintainers (contact to be added). Please include:
- Description of the issue
- Potential impact
- Steps to reproduce (if applicable)

Avoid public issues for unpatched vulnerabilities.

## Dependency Updates
We rely on .NET 10 preview. Periodically update dependencies; watch for security advisories in GitHub Dependabot once repository is public.

## Secrets
No secrets are stored in the repository. Environment variables (e.g., `STEAM_API_KEY`) must be supplied at runtime. If a secret is accidentally committed:
1. Remove it from history (git filter-repo or BFG).
2. Rotate the credential.
3. Open an issue or advisory noting the rotation.

## Data Integrity
Manifest (`manifest.json`) contains DB SHA256 hash. Startup validation recomputes and warns (or fails in strict mode) if mismatched.

## Future Hardening
- Signed releases including manifest signature.
- Checksum verification script during container startup.
- Optional defensive copy & integrity check of `games.db` before serving.

---
Document version: 2025-09-01
