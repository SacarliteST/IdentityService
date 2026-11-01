---
name: focused-feature-flow
description: Implement or revise an IdentityService feature with bounded analysis and a staged validation ladder, avoiding redundant full PostgreSQL integration-test runs.
---

# Focused feature flow

1. Read `AGENTS.md` and the concrete requirement. Read `README.md`, `CONCEPT.md`, or `ROADMAP.md` only when the task needs their context.
2. Check `git status`. Define the affected contract, vertical slice, data changes and security invariants before editing. Do not absorb unrelated changes.
3. Use `rg` to find one analogous feature and the direct consumers of any changed claim, route, DTO or error code. Avoid repository-wide speculative analysis after the call chain is understood.
4. Implement a coherent batch, then add focused tests. For a bug, a minimal reproducing test may come first.
5. Test behavior at API boundaries and security invariants; do not mirror private implementation details or generate exhaustive cases without risk justification.

## Validation ladder

- While coding: build only the affected project when useful.
- After the slice is coherent: run the relevant test class/filter.
- On failure: rerun only the failing test during diagnosis.
- After the task is fully implemented: run format once, build once, and the complete integration suite exactly once. The full suite is the final acceptance gate, not a check after each edit.

Use these final commands when the full gate is warranted:

```powershell
dotnet format IdentityService.sln --no-restore
dotnet build IdentityService.sln --no-restore
dotnet test IdentityService.sln --no-build --no-restore
```

Run formatting only at the end and inspect its diff. Do not start the Host, Docker infrastructure, code generation, commits, pushes, dependency installation, or cross-repository work unless required. Stop anything started for verification and report unverified checks plainly.
