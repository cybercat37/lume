# HTTP + DB Delivery Plan (M13-M21)

This plan turns `docs/proposals/http-db-reference.md` into incremental milestones that fit the
current Axom architecture (parser -> binder -> lowering -> interpreter/emitter -> CLI/tests).

Planning horizon: 12-20 weeks depending on team size and scope discipline.

## Scope Boundaries

- Keep delivery inside this repository to preserve compiler/runtime/test parity.
- Prefer runtime APIs first, then syntax sugar/DSL.
- Ship every milestone with interpreter + codegen parity and tests.
- Keep all new behavior opt-in until stability is proven.

## M13: HTTP Server Runtime Core

Objective: serve HTTP requests from Axom handlers without file-based routing yet.

Status: Partial (implemented slices)
- `axom serve <file.axom>` is available.
- Runtime host boots and serves `GET /health`.
- Serve validates source compilation before host startup.
- Graceful shutdown via Ctrl+C is implemented.

DoD:
- Runtime HTTP host abstraction exists and can boot from CLI.
- Request/response core types are available (`HttpRequest`, `HttpResponse`).
- Static route registration works for `GET/POST`.
- Errors map deterministically to status codes.

Implementation tasks:
- Add HTTP runtime module in `src/Axom.Runtime` using ASP.NET Core primitives.
- Add compiler/runtime bridge for handler invocation.
- Add minimal serialization helpers for plain text and JSON responses.
- Add CLI command surface for local server run (`axom serve` integration path).

Tests:
- Unit tests for request parsing and response mapping.
- Integration tests that boot a server and perform real HTTP requests.
- Snapshot diagnostics for invalid handler signatures.

## M14: File-Based Routing v1

Objective: implement route discovery from filesystem according to the new spec.

Status: Partial (implemented slices)
- `routes/**/*.axom` route scan/normalization is implemented.
- `index`, method suffixes, and dynamic params (`__id`, `__id_int`, etc.) are implemented.
- Compile-time conflict diagnostics are implemented, including overlap reason details.
- Dynamic route filename validation is implemented (invalid markers/identifiers are rejected).
- Discovered routes are mounted as runtime stubs; Axom route handler execution is pending.

DoD:
- `routes/**/*.axom` scan produces route table.
- `index` semantics and method suffixes are supported.
- Dynamic params via `__name` are supported.
- Compile-time conflict detection blocks ambiguous routes.

Implementation tasks:
- Add route scanner + normalizer in compiler layer.
- Add route conflict analyzer (same method + overlapping template domains).
- Emit precise diagnostics (method/path/file pair and overlap reason).
- Add examples under `examples/http/routes`.

Tests:
- Golden route-table tests.
- Conflict and collision diagnostics snapshots.
- End-to-end run tests for static and dynamic paths.

## M15: HTTP Client Stdlib v1

Objective: provide outbound HTTP calls with `Result`-based error handling.

DoD:
- `http.get` and `http.post` builtins are available.
- Headers and timeout are supported.
- Failures map to stable error categories.

Implementation tasks:
- Add builtin symbols and binder checks.
- Implement interpreter runtime using `HttpClient`.
- Emit codegen runtime helpers with the same semantics.
- Add response helpers (`status`, `headers`, `text`, `json` baseline).

Tests:
- Integration tests against a local test server.
- Parity tests for interpreter vs emitted C#.
- Timeout and network failure tests.

## M16: Auth Foundation (`@public`, `@auth`)

Objective: enforce endpoint protection with deterministic 401/403 behavior.

DoD:
- `@public` and `@auth` syntax is parsed and validated.
- Compile-time conflict rule (`@public` + `@auth`) is enforced.
- Runtime middleware performs authn/authz in fixed order.
- Standard 401/403 JSON responses include `requestId`.

Implementation tasks:
- Extend parser/binder with HTTP auth aspects.
- Introduce auth requirement metadata on lowered handlers.
- Add middleware hooks in runtime pipeline.
- Add secure-by-default config toggle.

Tests:
- Diagnostics tests for invalid/contradictory auth annotations.
- Integration tests for anonymous vs authenticated access.
- Contract tests for 401/403 payload format.

## M17: DB Runtime v1

Objective: add safe, parameterized database access with typed error channel.

DoD:
- DB connection configuration and lifecycle are available.
- `db.exec` and `db.query` support parameterized SQL only.
- DB errors map to a stable `DbError` model.

Implementation tasks:
- Pick first driver/backend (recommended PostgreSQL via Npgsql).
- Add runtime adapters and pooling policy.
- Add binder checks that prevent unsafe raw concatenation helpers.
- Add transaction baseline API (`begin/commit/rollback` or scoped helper).

Tests:
- Integration tests with ephemeral DB container.
- Retryable vs non-retryable error mapping tests.
- Transaction rollback and isolation smoke tests.

## M18: Typed SQL Interpolation v1

Objective: ship `sql"..."` with typed parameters and baseline typed result inference.

DoD:
- Parser supports SQL literal tokenization and interpolation markers.
- `@{p:T}` parameter typing is compile-time checked.
- Runtime receives prepared SQL + typed parameter metadata.
- Baseline `Sql[Row]` works; `Sql[T]` from `{T}` projection is enabled for flat records.

Implementation tasks:
- Add new syntax nodes for SQL literals and typed holes.
- Add binder inference rules (`Sql[Row]` fallback, `Sql[T]` where unambiguous).
- Add mapping runtime for by-name record projection.
- Add compile-time diagnostics for unknown fields and incompatible projections.

Tests:
- Parser/binder diagnostics snapshots.
- Golden SQL expansion tests.
- Runtime mapping tests (`MissingColumn`, `TypeMismatch`, `NullViolation`).

## M19: Security DSL (`security {}` + provider binding)

Objective: move auth provider definitions into Axom source with compile-time linking.

DoD:
- `security {}` supports `apikey` and `bearer` providers.
- `@auth(apikey:name)` / `@auth(bearer:name)` validates referenced providers.
- `default`, `require_by_default`, and `strict_intents` are enforced.

Implementation tasks:
- Add parser/binder model for `security` declarations.
- Add provider registry validation pass.
- Implement runtime provider adapters (API key and JWT/OIDC).
- Keep TOML overrides only for secret material and environment-specific values.

Tests:
- Compile-time validation tests (missing provider, invalid fields, duplicates).
- JWT integration tests with local JWKS fixture.
- API key flow tests.

## M20: Customer Docs Bundle + Protected `/docs`

Objective: generate and serve client-ready docs directly from service metadata.

DoD:
- Build emits internal spec JSON.
- Docs bundle is generated and served under configurable mount.
- Docs route is auth-protected according to policy.
- Download scripts (`api.sh`, `api.ps1`) are generated and served with attachment headers.

Implementation tasks:
- Add spec exporter from route/auth/type metadata.
- Add static bundle generator step and packaging mode (`embedded`/`filesystem`).
- Add hardened static file serving (no traversal, no directory listing).
- Add caching headers strategy for assets/index.

Tests:
- Snapshot tests for generated spec and scripts.
- HTTP tests for docs mount behavior and auth.
- Security tests for path traversal rejection.

## M21: Hardening, Performance, DX, Release

Objective: move from feature-complete to stable and releasable.

DoD:
- Full test suite stable in CI with HTTP + DB lanes.
- Performance baseline and regressions tracked.
- Docs/tutorial/spec are fully synchronized with implementation.
- Release notes and migration notes are prepared.

Implementation tasks:
- Expand fuzzing and negative tests for routing/auth/sql parsing.
- Add benchmarks for route matching and SQL binding.
- Improve diagnostics wording and fix-it guidance.
- Finalize versioning and packaging updates.

## Suggested Sprint Cadence

- Sprint 1: M13 core runtime skeleton + first integration test.
- Sprint 2: M14 route scanner + conflict diagnostics.
- Sprint 3: M15 HTTP client + parity tests.
- Sprint 4: M16 auth foundation.
- Sprint 5-6: M17 DB runtime v1.
- Sprint 7-8: M18 typed SQL interpolation.
- Sprint 9: M19 security DSL.
- Sprint 10: M20 docs bundle.
- Sprint 11: M21 hardening and release prep.

## Risk Register

- Scope creep across four axes (routing, auth, DB, docs) in one pass.
- Parser complexity increase from SQL + security DSL arriving too early.
- Runtime parity drift between interpreter and emitter.
- CI instability from network/DB integration tests if isolation is weak.

Mitigations:
- Keep strict milestone gates and defer sugar until runtime baseline is stable.
- Add parity tests for each new builtin/feature before broad rollout.
- Use deterministic local fixtures for JWT and DB integration lanes.

## Ready-to-Code Backlog (Noise-Minimized)

This backlog is intentionally kept in this single file to avoid scattered planning
artifacts. Use it as the implementation checklist.

### M13 Backlog (HTTP Server Runtime Core)

Compiler/runtime surfaces:
- Add runtime HTTP host types under `src/Axom.Runtime` (host, request, response).
- Add minimal compiler bridge for handler entrypoint generation in `src/Axom.Compiler/Emitting`.
- Keep syntax additions minimal in this step (no route DSL yet).

CLI + execution flow:
- Extend `src/axom` command handling to support a server launch mode (opt-in).
- Preserve existing `run/check/build` behavior unchanged for non-HTTP programs.

Tests to add:
- `tests/Axom.Tests/*HttpServer*` integration tests with real HTTP requests.
- Snapshot test for invalid HTTP handler signature diagnostics.

Acceptance gate:
- One example service starts and serves deterministic `GET /health` and `POST /echo`.

### M14 Backlog (File-Based Routing v1)

Compiler tasks:
- Add route file scanner in compiler layer (entrypoint + `routes/**/*.axom`).
- Normalize file path to `(method, template)` with `index` and suffix rules.
- Add dynamic segment model for `__param` and optional constraints.

Diagnostics tasks:
- Implement overlap checker for same-method templates.
- Emit diagnostics with both conflicting files and overlap explanation.

Tests to add:
- Route normalization golden tests.
- Conflict diagnostics snapshots (`static vs dynamic`, `dynamic vs dynamic`, flat vs nested collisions).

Acceptance gate:
- Conflict-free route tree compiles and serves all registered endpoints.

### M15 Backlog (HTTP Client Stdlib)

Stdlib/compiler tasks:
- Add builtin symbols (`http_get`, `http_post` or chosen canonical names) in binder.
- Add type symbols for response/error wrappers.

Runtime/emitter tasks:
- Implement interpreter path using `HttpClient`.
- Implement codegen helper runtime with same timeout/header semantics.
- Ensure deterministic mapping into `Result` errors.

Tests to add:
- Local server-driven tests for success, timeout, DNS/connect failure.
- Parity tests: same Axom program in interpreter and emitted C#.

Acceptance gate:
- Outbound calls are stable in CI and return predictable errors.

### M16 Backlog (Auth Foundation)

Syntax/binder tasks:
- Parse `@public` and `@auth` as builtin endpoint aspects.
- Add conflict validation (`@public` + any `@auth*` on same target).

Runtime tasks:
- Add auth middleware stage in request pipeline.
- Add fixed JSON payload format for 401/403 with `requestId`.

Tests to add:
- Compile-time diagnostics for invalid annotations.
- Integration tests for anonymous/protected endpoints.

Acceptance gate:
- Protected endpoints cannot be reached without auth context, with deterministic responses.

### M17 Backlog (DB Runtime v1)

Runtime tasks:
- Add DB runtime adapter package (PostgreSQL-first).
- Add connection config loading and pool lifecycle.
- Add `query/exec` APIs with parameter bag abstraction.

Compiler tasks:
- Add binder checks for parameterized execution APIs.
- Add stable `DbError` type mapping contract.

Tests to add:
- Container-backed integration tests (startup/teardown deterministic).
- Transaction smoke tests (`commit`, `rollback`).

Acceptance gate:
- Basic CRUD scenario passes with parameterized queries only.

### M18 Backlog (Typed SQL Interpolation)

Parser tasks:
- Add SQL literal syntax node and typed hole parser for `@{p:T}`.
- Add diagnostics for malformed placeholders and unsupported forms.

Binder tasks:
- Infer `Sql[Row]` baseline and `Sql[T]` on unambiguous projection.
- Validate record projection fields for flat records.

Runtime tasks:
- Convert typed SQL literal to prepared statement payload + metadata.
- Add row-to-record mapping with strict error categories.

Tests to add:
- Parser diagnostics snapshots for malformed SQL holes.
- Golden tests for SQL expansion and parameter binding metadata.
- Runtime mapping error tests (`MissingColumn`, `TypeMismatch`, `NullViolation`).

Acceptance gate:
- Typed parameter binding works with compile-time checks and runtime-safe execution.

### M19 Backlog (Security DSL)

Parser/binder tasks:
- Add `security {}` declarations and provider model nodes.
- Validate provider references used by `@auth(provider:...)`.

Runtime tasks:
- Implement API key provider adapter.
- Implement JWT/OIDC provider adapter with deterministic validation errors.

Tests to add:
- Compile-time tests for duplicate/missing providers and invalid fields.
- Integration tests with local JWKS fixture.

Acceptance gate:
- Provider-bound auth works end-to-end with compile-time safety.

### M20 Backlog (Docs Bundle)

Build tasks:
- Add internal spec JSON exporter for endpoint/auth/type metadata.
- Add docs bundle generator and packaging mode switch (`embedded`/`filesystem`).

Runtime tasks:
- Serve docs mount with secure static file rules.
- Add download endpoint headers for scripts.

Tests to add:
- Snapshot tests for generated JSON/spec/scripts.
- HTTP tests for auth-protected docs mount and path traversal rejection.

Acceptance gate:
- `/docs` behaves predictably and securely in both packaging modes.

### M21 Backlog (Hardening and Release)

Stability tasks:
- Expand fuzz/negative tests for routing/auth/sql grammar.
- Add perf benchmarks for route match and SQL bind paths.
- Improve diagnostics text and fix suggestions.

Release tasks:
- Update `README.md`, `docs/spec.md`, `docs/tutorial.md`, `roadmap.md` for shipped scope.
- Prepare migration notes and release checklist.

Acceptance gate:
- CI is green across unit/integration/perf-smoke lanes and docs are aligned.
