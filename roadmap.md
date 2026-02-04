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
- Pattern match v1 (literals, wildcard, identifiers, tuples)
- Records v1 and sum types v1
- Builtins: print, println, input, len, abs, min, max

Runtime + tooling:
- Interpreter and codegen (C# emitter)
- Dedicated lowering pass with lowered nodes
- CLI: check, build, run (+ out/verbosity flags)
- Golden + snapshot tests and fuzz harness
- Compilation cache and large input guardrail
- NuGet tool packaging + CI + shell completions

## Gap Analysis (Spec vs Implementation)

| Area | Spec Expectation | Current Status | Notes / Gaps |
| --- | --- | --- | --- |
| Pattern match v1 | Exhaustive match, diagnostics | Implemented | Prior roadmap listed as pending; now consolidated here |
| Float type v1 | Float literals + ops | Implemented | Prior roadmap listed as next focus; now consolidated here |
| Tail-call optimization | Compiler optimizes tail calls | Partial | Interpreter supports TCO; codegen pending |
| Comments vs intent | No traditional comments; intent is the planned alternative | Implemented | Spec + intent proposal aligned |
| Result/Option + ? | Explicit error types + propagation | Not implemented | Spec and tutorial list planned |
| Collections + iterators | List/Map + combinators | Not implemented | Spec + proposal exist |
| Tuples (general) | Tuple literals + destructuring | Partial | Match tuples exist; general tuples planned |
| Generics | Minimal generics | Not implemented | Roadmap v0.5 item |
| Modules/imports | One-file modules + import | Not implemented | Spec planned |
| String interpolation | f"...{expr}..." | Not implemented | Spec planned |
| Intent annotations | @intent + effect checks + docs | Not implemented | Step 14 pending |
| Concurrency model | scope/spawn/join/cancel/par | Not implemented | Spec planned |
| .NET interop | Direct calls + NuGet | Not implemented | Spec planned |
| Pipeline operator + combinators | \|> and combinators | Not implemented | Proposal only |

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

### M2: Generics v1 + Tuples + Destructuring
Objective: unlock reusable types and tuple-first programming.

DoD:
- Generic parameters on functions and types.
- Tuple literals, tuple types, destructuring in let and match.
- Diagnostics for arity/type mismatches.

Key tasks:
- Decide monomorphization vs boxing strategy for codegen.
- Extend binder/type checker for tuple unification.

### M3: Collections v1 + Iteration
Objective: core list/map literals and iteration helpers.

DoD:
- List literal syntax + index access with diagnostics.
- Map literal syntax (String keys) + lookups.
- Stdlib each/map/fold/filter for lists.

Key tasks:
- Interpreter support for list/map semantics.
- Codegen mapping to C# collections.

### M4: Error Handling Core
Objective: Result/Option and propagation semantics.

DoD:
- Result/Option types in stdlib.
- Postfix ? works for Result and Option.
- .unwrap() with clear diagnostics.

Key tasks:
- Control-flow lowering for early return.
- Update diagnostics snapshots for error propagation.

### M5: Pattern Match v2
Objective: match guards + list patterns + richer exhaustiveness.

DoD:
- Guards and list/rest patterns.
- Exhaustiveness and unreachable diagnostics for new pattern types.
- Interpreter/codegen parity.

Key tasks:
- Extend pattern AST/binder.
- Improve exhaustiveness analysis.

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
- par for CPU parallelism.

Key tasks:
- Runtime primitives and scheduler model.
- Effect tagging for suspensive functions.

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
