---
name: identity-development
description: Develop IdentityService authentication, JWT, token exchange, user administration, audit, EF Core, public contracts, and typed clients with explicit cross-service security review.
---

# IdentityService development

Read `AGENTS.md` first. For token or integration work also read the relevant sections of `README.md` and root `../../PLATFORM.md`.

## Architecture routing

- `Web/Features/<Area>/<Feature>`: endpoint, command/query, handler and validator vertical slices.
- `Contracts`: public DTOs, enums, constants and `ApiRoutes`.
- `Domain`: entities and invariants.
- `Data`: EF Core configuration, migrations and seeding.
- `Client`: typed HTTP client kept in sync with public API changes.
- `Common`: shared CQRS/result primitives only.
- `Tests/IntegrationTests`: PostgreSQL Testcontainers behavior tests.

Keep public routes centralized and describe all endpoint outcomes. Validation returns structured `422`; missing resources use `404`, state conflicts `409`, and authorization remains `401`/`403`.

## Security boundary

Treat issuer, audience, signing, JWKS, roles, claims, token exchange and refresh-token behavior as cross-service contracts. Before changing them, locate consumers in Education and SqlModule, but do not edit those repositories without explicit scope. Never log or return tokens, passwords, hashes, private keys or request bodies containing credentials.

Multi-record identity operations and their audit records must commit atomically through the same context. Store only refresh-token hashes. Preserve the last-active-admin and self-protection rules described in `AGENTS.md`.

Generate EF migrations with `unsandboxed-codegen` outside the sandbox. Do not automatically replace the checked-in root `swagger.json`; follow the repository rule and user request.

Follow `$focused-feature-flow`: targeted tests while iterating, then at most one justified full integration run.
