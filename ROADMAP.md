# Lume Roadmap (12 Main Steps)

This roadmap defines the major milestones for building the Lume compiler/runtime/CLI.
Each step is meant to be completed sequentially and validated with tests.

1) Pipeline base
   Lexer, parser, AST, diagnostics, minimal emitter.
   Definition of Done:
   - All sub-steps in `STEP1_PIPELINE.md` complete.
   - `dotnet test` passes with new pipeline tests.

2) Core syntax expansion
   Numeric literals, variables, assignments, block statements, and basic expressions.
   Definition of Done:
   - Parser handles new syntax with diagnostics.
   - Binder validates symbol usage and reports errors.
   - Tests cover happy paths and failure cases.

3) Parser robustness
   Error recovery, synchronization points, and clear diagnostics.
   Definition of Done:
   - Parser recovers from at least 3 common error patterns.
   - Diagnostics include expected token/context.
   - Failing input still yields a syntax tree.

4) Binding and scope
   Symbol resolution, scope rules, and semantic diagnostics.
   Definition of Done:
   - Nested scopes work with shadowing rules.
   - Undefined symbols produce diagnostics with spans.
   - Tests cover scope boundaries.

5) Type system foundation
   Primitive types, conversions, type inference for literals and variables.
   Definition of Done:
   - Type checker enforces rules for literals and assignments.
   - Implicit conversions are explicit in diagnostics.
   - Tests cover valid/invalid type scenarios.

6) Interpreter runtime
   Execute AST directly for fast feedback and feature validation.
   Definition of Done:
   - Interpreter can run a multi-statement program.
   - Errors surface as diagnostics (not exceptions).
   - Tests verify runtime outputs.

7) Code generation v1
   Structured output for multiple statements and typed values.
   Definition of Done:
   - Emitter supports statements, expressions, and types.
   - Generated code compiles for supported programs.
   - Tests validate emitted output.

8) Standard library minimal
   Console I/O, math, string helpers, and collections baseline.
   Definition of Done:
   - Minimal API surface documented.
   - Library functions tested in integration tests.
   - Usage errors provide clear diagnostics.

9) CLI UX expansion
   Commands for check/format/run/build with rich diagnostics output.
   Definition of Done:
   - New CLI commands wired and documented.
   - Non-zero exit codes for failures.
   - Tests verify CLI output/exit codes.

10) Test strategy hardening
   Golden files, snapshot tests, and parser fuzzing.
   Definition of Done:
   - Golden tests added for codegen outputs.
   - Snapshot tests cover diagnostics formatting.
   - Fuzzing runs in CI or pre-merge.

11) Performance and caching
   Incremental compilation, caching, and efficient parsing.
   Definition of Done:
   - Incremental parse/build works on unchanged input.
   - Caching reduces repeated work measurably.
   - Benchmarks captured for baseline.

12) Tooling and distribution
   Packaging, versioning, docs, examples, and CI.
   Definition of Done:
   - Release artifacts produced via CI.
   - Versioning policy documented.
   - Examples verified as part of build/test.

## Current progress
- Step 1: in progress (see `STEP1_PIPELINE.md`)
