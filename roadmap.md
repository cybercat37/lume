# Axom Roadmap (Complete)

This roadmap consolidates the language specification, tutorial status, and the
existing step-based roadmap into a single plan that includes both language
features and technical work. It is ordered by priority, not by dates.

Sources used for consolidation:
- docs/spec.md
- docs/tutorial.md
- README.md
- AGENTS.md

## Current Baseline (Implemented)

Language core:
- Lexer + parser with recovery
- Statements/expressions, blocks, scopes, assignments
- Primitive types: Int, Float, Bool, String
- Operators: arithmetic, comparison, logical, unary, string concatenation
- Numeric conversions: float(Int), int(Float)
- Functions and lambdas (first-class, captures)
- Tuple destructuring in let declarations
- Tuple type syntax in function signatures
- Pattern match v1 (literals, relational, wildcard, identifiers, tuples)
- Pattern match v1 (record patterns)
- Pattern match v2 (guards for variants/records)
- List literals (non-empty, uniform type)
- Map literals (string keys, uniform value type)
- Records v1.1 (`with` update + spread copy) and sum types v1
- Option/Result patterns with ? and unwrap()
- Generics v1 (functions only)
- Builtins: print, println, input, len, abs, min, max, sleep, rand_float, rand_int, rand_seed

Runtime + tooling:
- Interpreter and codegen (C# emitter)
- Dedicated lowering pass with lowered nodes
- Concurrency runtime prototype (scope/spawn/join)
- CLI: check, build, run (+ out/verbosity flags)
- Golden + snapshot tests and fuzz harness
- Compilation cache and large input guardrail
- NuGet tool packaging + CI + shell completions

## Gap Analysis (Spec vs Implementation)

Status labels used across docs: `Implemented`, `Partial`, `Planned`.

| Area | Spec Expectation | Current Status | Notes / Gaps |
| --- | --- | --- | --- |
| Pattern match v1 | Exhaustive match, diagnostics | Implemented | Prior roadmap listed as pending; now consolidated here |
| Float type v1 | Float literals + ops | Implemented | Prior roadmap listed as next focus; now consolidated here |
| Tail-call optimization | Compiler optimizes tail calls | Partial | Interpreter supports TCO; codegen pending |
| Comments vs intent | No traditional comments; intent is the planned alternative | Implemented | Spec + intent proposal aligned |
| Result/Option + ? | Explicit error types + propagation | Implemented | `?` and `.unwrap()` available for `Result` and `Option` |
| Collections + iterators | List/Map + combinators | Partial | List/Map literals plus `map`/`filter`/`fold`/`each` builtins implemented; range and richer iterator APIs remain planned |
| Tuples (general) | Tuple literals + destructuring | Partial | Match tuples exist; general tuples planned |
| Generics | Minimal generics | Partial | Function generics are implemented; type generics remain planned |
| Records update syntax | One obvious update form | Implemented | `target with { ... }` is the only update syntax; spread literals are copy-only |
| Modules/imports | One-file modules + import | Implemented | Resolver v1 is implemented (imports, pub visibility, wildcard rejection, cycle/conflict diagnostics, aliases for `import ... as ...` and `from ... import ... as ...`) |
| String interpolation | f"...{expr}..." | Partial | `f"...{expr}..."` interpolation is implemented with escaped braces and baseline `:specifier` formatting; advanced formatting controls remain planned |
| Time/random builtins | sleep + random helpers | Implemented | `sleep(ms)`, `rand_float()`, `rand_int(max) -> Result`, `rand_seed(seed)` are implemented in interpreter+codegen |
| Intent annotations | @intent + effect checks + docs | Planned | Step 14 pending |
| Concurrency model | scope/spawn/join/cancel + channels | Partial | Runtime prototype exists for spawn/join; channel v1 send/recv + strict `recv -> Result` + scope-close unblock + bounded capacity + baseline cancel propagation are implemented |
| .NET interop | Direct calls + NuGet | Partial | `dotnet.call<T>` / `dotnet.try_call<T>` implemented with `System.Math` whitelist |
| Pipeline operator + combinators | \|> and combinators | Partial | Value pipe `|>` implemented; combinator syntax remains proposal-only |

## Milestones (Priority Ordered)

### M0: Alignment and Roadmap Hygiene
Objective: make docs and roadmaps consistent with the actual implementation.

DoD:
- README/spec/tutorial/step docs agree on what is implemented vs planned.
- Prior roadmap entries reflect current reality.
- Confirm intent annotations as the only documentation path.

Key tasks:
- Reconcile tutorial “planned” sections with tests and README.
- Ensure docs/tools do not reference traditional comments.

### M1: Compiler Modularity + Lowering Pass (Done)
Objective: introduce an explicit lowering stage to decouple binding from execution.

DoD:
- Dedicated Lowerer transforming BoundProgram to a lowered form.
- Interpreter and codegen consume lowered nodes.
- Public entry points remain stable (SyntaxTree.Parse, Interpreter.Run).

Key tasks:
- Define lowered node set and conversions.
- Update diagnostics flow to remain consistent.

### M2: Generics v1 + Tuples + Destructuring (Done)
Objective: unlock reusable functions and tuple-first programming.

DoD:
- Generic parameters on functions.
- Tuple type syntax in function signatures and tuple destructuring in let/match.
- Diagnostics for arity/type mismatches.

Key tasks:
- Extend generics from functions to types (follow-up).
- Continue tuple support beyond current syntax/usage.

### M3: Collections v1 + Iteration (Mostly Complete)
Objective: core list/map literals and iteration helpers.

DoD:
- List literal syntax + index access with diagnostics.
- Map literal syntax (String keys) + lookups.
- Stdlib each/map/fold/filter for lists.

Status:
- List/map literal support is implemented.
- Iterator combinators (`map`/`filter`/`fold`/`each`) are implemented.
- Remaining follow-up: `range` and richer iterator APIs.

### M4: Error Handling Core (Done)
Objective: Result/Option and propagation semantics.

DoD:
- Result/Option types in stdlib.
- Postfix `?` works for `Result` and `Option`.
- `.unwrap()` with clear diagnostics.

Key tasks:
- Extend diagnostics quality and examples for complex propagation chains.

### M5: Pattern Match v2 (Partial)
Objective: match guards + list patterns + richer exhaustiveness.

DoD:
- Guards and list/rest patterns.
- Exhaustiveness and unreachable diagnostics for new pattern types.
- Interpreter/codegen parity.

Status:
- Guards for variants/records are implemented.
- List/rest patterns and related exhaustiveness work remain pending.

### M6: Modules, Imports, Visibility (Done)
Objective: establish module boundary and namespacing model.

DoD:
- One file = one module with path-based names (`a/b/c.axom` -> `a.b.c`).
- Import forms implemented for v1: `import mod`, `from mod import name[, ...]`.
- Alias forms are supported for `import ... as ...` and `from ... import ... as ...`.
- `pub` visibility enforced across module boundaries (default private).
- Wildcard imports are rejected in v1.
- Import cycles produce deterministic diagnostics with cycle path.
- CLI supports multi-file compilation from an entrypoint.

Key tasks:
- Track additional diagnostics hardening as ongoing maintenance.

Implementation notes (v1 scope):
- Top-level declarations only (`fn`, `type`, `let`) participate in exports.
- No explicit export list syntax; `pub` is the only export mechanism.
- Runtime/codegen emits a single combined program after module resolution.
- `from ... import ... as ...` supports aliasing exported values and types.

### M7: String Interpolation + Formatting
Objective: f-strings with expressions and predictable escaping.

DoD:
- f"...{expr}..." syntax.
- Interpolated strings type-check as String.
- Codegen and interpreter consistency.

Status:
- Baseline interpolation syntax is implemented (`f"...{expr}..."`) with escaped braces (`{{` / `}}`).
- Interpolated expressions are stringified consistently through builtin `str(...)`.
- Baseline formatting specifiers are supported via `f"...{expr:specifier}..."` (`IFormattable` + `upper`/`lower` string specifiers).
- Advanced formatting controls and extended spec coverage remain follow-up work.

### M8: Intent Annotations + Effect Inference
Objective: intent metadata for docs and diagnostics.

DoD:
- @intent on blocks and let bindings.
- Basic effect inference (db/network/fs/time/random).
- Warnings on mismatch; docs output path.

Key tasks:
- Decide effect taxonomy and warning codes.
- Snapshot tests for warnings.

### M9: Concurrency + Parallelism (Mostly Complete)
Objective: structured concurrency and explicit parallel execution.

DoD:
- scope/spawn/join semantics with cancellation.
- Suspensive function typing (planned `ValueTask` mapping; current runtime/codegen path uses `Task`).
- CPU parallelism via structured `scope` + `spawn` patterns.
- Typed channel messaging (`channel<T>`, `send`, blocking `recv`).
- Strict channel error handling (`recv -> Result<T, String>`, explicit handling via `?`/`match`).
- Bounded channel capacity (`channel<T>(N)`, default capacity).

Remaining follow-up tasks:
- Runtime primitives and scheduler model.
- Effect tagging for suspensive functions.
- Extend cancellation propagation behavior beyond baseline semantics.
- Add advanced buffering/backpressure strategies beyond bounded FIFO capacity.

### M10: .NET Interop Surface (Partial)
Objective: controlled access to .NET APIs and NuGet packages.

DoD:
- Direct .NET call syntax (`dotnet.call<T>`, `dotnet.try_call<T>`).
- NuGet reference flow documented and tested.
- Exception capture mapped to `Result` via `dotnet.try_call<T>`.

Key tasks:
- Expand interop whitelist beyond `System.Math`.
- Interop safety rules + diagnostics.

### M11: Tooling + Developer Experience
Objective: consistent UX and better editor support.

DoD:
- CLI help polish and stable diagnostics formatting.
- VSCode extension updated with latest syntax.
- Optional LSP or richer highlighting features.

Key tasks:
- Keep docs/tutorial in sync per release.

### M12: Aspects and Runtime Policies (Proposed)
Objective: evolve `@...` from passive metadata into concrete, opt-in behavior
for observability, reliability, and integration boundaries.

DoD (v1):
- Builtin aspect tags use identifier syntax (e.g. `@logging`) with validation,
  instead of free-form strings.
- First runtime-capable aspects are available end-to-end in interpreter + codegen.
- Aspect target rules are explicit and diagnostics are deterministic.
- Baseline tests and examples exist for each shipped builtin aspect.

MVP priorities (execution order):
1. `@logging` (invocation, args, return value, optional duration)
2. `@retry` + `@timeout` for reliability boundaries
3. `@webhook` + output serialization for API-facing flows
4. `@mqtt_publish` / `@mqtt_subscribe` for messaging workflows

Proposal backlog:

### Aspect syntax and model
- Promote aspect tags to builtin identifiers (e.g. `@logging`, `@metrics`) instead of free-form strings.
- Keep an extension escape hatch (`@custom("team.policy")`) for project-specific tooling.
- Allow multiple aspects on the same declaration with deterministic order.
- Define aspect targets explicitly (`fn`, `block`, `let`, future `type`) and reject invalid placements.
- Add per-aspect arguments with typed schemas (e.g. `@retry(max: 3, backoff: "exp")`).

### Observability and diagnostics
- `@logging`: log invocation, args, return value, and optional duration.
- `@trace`: emit trace spans with nested call structure and correlation ids.
- `@metrics`: automatic counters/timers/histograms around annotated code paths.
- `@audit`: append structured audit events for sensitive operations.
- PII-safe logging policies (masking/redaction for selected params/fields).

### Reliability and control flow
- `@retry` for transient failures with bounded attempts and backoff strategies.
- `@timeout` for bounded execution windows on functions/blocks.
- `@circuit_breaker` wrappers for unstable external integrations.
- `@fallback` handlers for graceful degradation paths.
- `@idempotent` hints + diagnostics for risky side effects in retried code.

### Transactions and consistency
- `@transaction` with begin/commit/rollback semantics over supported backends.
- `@saga` orchestration with compensating actions for distributed workflows.
- `@outbox` pattern support for reliable event publishing.
- `@unit_of_work` boundary management for batched state changes.
- Conflict/isolation diagnostics (best-effort static checks + runtime hooks).

### Web/API-oriented capabilities
- `@http` function exposure for lightweight endpoint declaration.
- Route attributes (`@get`, `@post`, `@put`, `@delete`) with typed params.
- Request validation aspects (`@validate`) with auto 4xx mapping.
- `@auth` / `@authorize` policies for role/scope checks.
- `@rate_limit` and `@cache` for endpoint-level governance/perf.

### Messaging and external integrations
- `@mqtt_publish` / `@mqtt_subscribe` for topic-driven workflows.
- Connection/session policies for MQTT (`qos`, retained messages, reconnect strategy).
- `@mail` / `@email` aspects for templated outbound notifications.
- Delivery diagnostics (accepted/rejected/deferred) mapped to Result-style outcomes.
- `@webhook` emit/receive aspects with signature verification and replay protection.

### Lifecycle and extension hooks
- `@before` / `@after` hooks around function invocation boundaries.
- `@on_error` hooks for centralized failure handling and alerting.
- Domain event hooks (`@on_create`, `@on_update`, `@on_delete`) for entity workflows.
- Build/runtime plugin hooks for custom organization policies.
- Ordering/composition rules when multiple hooks/aspects are stacked.

### Serialization and contracts
- Automatic output serialization (`@serialize("json")`, future `yaml/msgpack`).
- Content negotiation hooks (`Accept`/`Content-Type`) for API contexts.
- Schema derivation from Axom types for request/response contracts.
- Versioned contract aspects (`@api_version("v1")`) for compatibility.
- Stable field naming/renaming directives and backward-compat diagnostics.

### AI-assisted intent validation (non-deterministic lane)
- `axom intent-check` command that compares natural-language intent vs code behavior.
- Report classes: `supported`, `partial`, `contradicted` with rationale.
- Structured prompting based on AST + inferred effects (not raw text only).
- CI advisory mode first, optional strict mode later.
- Model/provider abstraction with deterministic fallback checks.

## Technical Backlog (Cross-Cutting)

- Determinism and caching verification across parse/bind/lower/emit.
- Reduce allocations in lexer/parser (noted hotspots).
- Expand golden/snapshot coverage for new features.
- Improve diagnostics clarity and error recovery around new syntax.
- Clarify release/versioning process for multi-platform artifacts.

## Not in Scope / Uncommitted Proposals

- Dedicated pipeline-combinator expression syntax (proposal only).
- Alternative comment systems beyond intent annotations.
- Advanced effect system beyond pragmatic diagnostics.
