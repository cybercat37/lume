# Axom Roadmap (v0.5 – Next Minor)

This roadmap defines the next minor release after v0.4. It focuses on
language features already outlined in the spec/tutorial and keeps scope
tight enough for end-to-end tests and docs updates.

## Baseline
- Steps 1–13 are complete (pipeline through functions/lambdas).
- Step 14 (Intent annotations) is pending and deferred past v0.5.

## v0.5 Goals (medium detail)

1) Pattern match v1
   Implement `match` as the primary branching construct.
   Definition of Done:
   - Parser supports `match` with arms and `->` result expressions.
   - Binder/type checker validate arm patterns and types.
   - Exhaustiveness and unreachable arm diagnostics are enforced.
   - Interpreter/codegen execute `match` with correct semantics.
   - Tests cover literals, wildcards, nested patterns, and diagnostics.

2) Records (product types) v1
   Add `type` records, construction, and field access.
   Definition of Done:
   - Syntax: `type User { name: String, age: Int }` with trailing commas allowed.
   - Binder/type checker validate field existence and initialization.
   - Interpreter and codegen support record construction and field reads.
   - Diagnostics for missing/duplicate fields and type mismatches.
   - Tests cover construction, access, and failure cases.
   Status: complete.
   Future: consider constructor-style `User(...)` literals.

3) Sum types (enums with payload) v1
   Add `type Result<T, E> { Ok(T) Error(E) }`-style declarations.
   Definition of Done:
   - Parser handles variant declarations with optional payloads.
   - Binder/type checker resolve constructors and payload types.
   - Match exhaustiveness checks include sum type variants.
   - Interpreter and codegen support constructing and matching on variants.
   - Tests cover constructor calls, matches, and diagnostics.

4) Generics v1
   Enable minimal `<T>` generics for functions and types.
   Definition of Done:
   - Parser supports generic parameters in type and function declarations.
   - Type checker resolves generic instantiations and basic inference.
   - Diagnostics for arity mismatches and unbound type parameters.
   - Interpreter/codegen handle monomorphized or boxed generics consistently.
   - Tests cover identity-style and nested generic scenarios.

5) Tuples and destructuring
   Introduce tuple literals, match patterns, and destructuring.
   Definition of Done:
    - Syntax: `(1, "hi")` and `let (x, y) = pair`.
    - Type checker validates tuple shapes and element access.
    - Interpreter/codegen support tuple construction and destructuring.
    - Diagnostics for arity mismatches and invalid patterns.
    - Tests cover tuple usage in expressions, match patterns, and bindings.

6) Error handling core
   Implement `Result`/`Option` idioms and propagation.
   Definition of Done:
   - Standard library exposes `Result` and `Option` constructors and helpers.
   - Postfix `?` works for `Result` and `Option` with correct early return.
   - `.unwrap()` available with clear diagnostics on misuse.
   - Match examples in docs compile and run.
   - Tests cover propagation, matching, and error diagnostics.

7) Collections v1 + iterator combinators
   Add basic list/map literals and iteration helpers.
   Definition of Done:
   - List literal `[1, 2, 3]` and index access with diagnostics.
   - Map literal `{ "k" -> "v" }` with String keys (initial restriction).
   - Stdlib provides `each`, `map`, `fold`, `filter` for lists.
   - Interpreter/codegen support list/map operations.
   - Tests cover iteration semantics and out-of-range diagnostics.

8) CLI args + docs sync
   Wire program arguments and keep docs/spec aligned.
   Definition of Done:
   - CLI exposes argv to the program (design documented and tested).
   - `docs/spec.md` and `docs/tutorial.md` updated with v0.5 features.
   - README roadmap section reflects v0.5 scope and status.

## Out of scope for v0.5
- Intent annotations/effect validation (Step 14).
- Structured concurrency (`scope`, `spawn`, `task.join`) and cancellation.
- CPU parallelism (`par`) semantics and runtime.
- .NET interop surface (direct calls/NuGet) beyond current codegen.
- Advanced effect system beyond pragmatic diagnostics.

## Status tracking
- Update progress per step as features land.
- See `docs/roadmap/STEP15_PATTERN_MATCH.md` for the next detailed step breakdown.
