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
- Pattern match v1 (literals, wildcard, identifiers, tuples)
- Pattern match v1 (record patterns)
- Pattern match v2 (guards for variants/records)
- List literals (non-empty, uniform type)
- Map literals (string keys, uniform value type)
- Records v1 and sum types v1
- Option/Result patterns with ? and unwrap()
- Generics v1 (functions only)
- Builtins: print, println, input, len, abs, min, max

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
| Collections + iterators | List/Map + combinators | Partial | List/Map literals implemented; iterator combinators remain planned |
| Tuples (general) | Tuple literals + destructuring | Partial | Match tuples exist; general tuples planned |
| Generics | Minimal generics | Partial | Function generics are implemented; type generics remain planned |
| Modules/imports | One-file modules + import | Planned | Spec planned |
| String interpolation | f"...{expr}..." | Planned | Spec planned |
| Intent annotations | @intent + effect checks + docs | Planned | Step 14 pending |
| Concurrency model | scope/spawn/join/cancel | Partial | Runtime prototype + parser/binder stubs exist; full semantics pending |
| .NET interop | Direct calls + NuGet | Planned | Spec planned |
| Pipeline operator + combinators | \|> and combinators | Planned | Proposal only |

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

### M3: Collections v1 + Iteration (Partial)
Objective: core list/map literals and iteration helpers.

DoD:
- List literal syntax + index access with diagnostics.
- Map literal syntax (String keys) + lookups.
- Stdlib each/map/fold/filter for lists.

Status:
- List/map literal support is implemented.
- Iterator combinators remain pending.

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

### M6: Modules, Imports, Visibility
Objective: establish module boundary and namespacing model.

DoD:
- One file = module, import syntax, aliasing.
- pub/private visibility enforcement.
- CLI supports multi-file compilation.

Key tasks:
- Module resolution rules.
- File system layout conventions.

### M7: String Interpolation + Formatting
Objective: f-strings with expressions and predictable escaping.

DoD:
- f"...{expr}..." syntax.
- Interpolated strings type-check as String.
- Codegen and interpreter consistency.

### M8: Intent Annotations + Effect Inference
Objective: intent metadata for docs and diagnostics.

DoD:
- @intent on blocks and let bindings.
- Basic effect inference (db/network/fs/time/random).
- Warnings on mismatch; docs output path.

Key tasks:
- Decide effect taxonomy and warning codes.
- Snapshot tests for warnings.

### M9: Concurrency + Parallelism
Objective: structured concurrency and explicit parallel execution.

DoD:
- scope/spawn/join semantics with cancellation.
- Suspensive function typing (ValueTask mapping).
- CPU parallelism via structured `scope` + `spawn` patterns.

Key tasks:
- Runtime primitives and scheduler model.
- Effect tagging for suspensive functions.
- Concurrency syntax stubs (scope/spawn/join) in parser/binder.

### M10: .NET Interop Surface
Objective: controlled access to .NET APIs and NuGet packages.

DoD:
- Direct .NET call syntax.
- NuGet reference flow documented and tested.
- DotNet.try interop for exception capture.

Key tasks:
- Interop safety rules + diagnostics.

### M11: Tooling + Developer Experience
Objective: consistent UX and better editor support.

DoD:
- CLI help polish and stable diagnostics formatting.
- VSCode extension updated with latest syntax.
- Optional LSP or richer highlighting features.

Key tasks:
- Keep docs/tutorial in sync per release.

## Technical Backlog (Cross-Cutting)

- Determinism and caching verification across parse/bind/lower/emit.
- Reduce allocations in lexer/parser (noted hotspots).
- Expand golden/snapshot coverage for new features.
- Improve diagnostics clarity and error recovery around new syntax.
- Clarify release/versioning process for multi-platform artifacts.

## Not in Scope / Uncommitted Proposals

- Pipeline operator and combinator expressions (proposal only).
- Alternative comment systems beyond intent annotations.
- Advanced effect system beyond pragmatic diagnostics.
