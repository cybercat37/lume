# Lume Roadmap (12 Main Steps)

This roadmap defines the major milestones for building the Lume compiler/runtime/CLI.
Each step is meant to be completed sequentially and validated with tests.

1) Pipeline base
   Lexer, parser, AST, diagnostics, minimal emitter.
   Definition of Done:
   - All sub-steps in `STEP1_PIPELINE.md` complete.
   - `dotnet test` passes with new pipeline tests.
   - `make test-pipeline` validates Step 1 end-to-end (`CompilerPipelineTests.Compile_print_string_generates_console_write`).

2) Core syntax expansion
   Numeric literals, variables, assignments, block statements, and basic expressions.
   Definition of Done:
   - Parser handles new syntax with diagnostics.
   - Binder validates symbol usage and reports errors.
   - Tests cover happy paths and failure cases.
   - See `STEP2_CORE_SYNTAX.md` for the 12 sub-steps.

3) Parser robustness
   Error recovery, synchronization points, and clear diagnostics.
   Definition of Done:
   - Parser recovers from at least 3 common error patterns.
   - Diagnostics include expected token/context.
   - Failing input still yields a syntax tree.
   - See `STEP3_PARSER_ROBUSTNESS.md` for the 12 sub-steps.

4) Binding and scope
   Symbol resolution, scope rules, and semantic diagnostics.
   Definition of Done:
   - Nested scopes work with shadowing rules.
   - Undefined symbols produce diagnostics with spans.
   - Tests cover scope boundaries.
   - See `STEP4_BINDING_SCOPE.md` for the 12 sub-steps.

5) Type system foundation
   Primitive types, conversions, type inference for literals and variables.
   Definition of Done:
   - Type checker enforces rules for literals and assignments.
   - Implicit conversions are explicit in diagnostics.
   - Tests cover valid/invalid type scenarios.
   - See `STEP5_TYPE_SYSTEM.md` for the 12 sub-steps.

6) Interpreter runtime
   Execute AST directly for fast feedback and feature validation.
   - See `STEP6_INTERPRETER.md` for the 12 sub-steps.
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
   - See `STEP7_CODEGEN.md` for the 12 sub-steps.

8) Standard library minimal
   Console I/O, math, string helpers, and collections baseline.
   Definition of Done:
   - Minimal API surface documented.
   - Library functions tested in integration tests.
   - Usage errors provide clear diagnostics.
   - See `STEP8_STDLIB.md` for the 12 sub-steps.

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
- Step 1: complete (see `STEP1_PIPELINE.md`)
- Step 2: complete (see `STEP2_CORE_SYNTAX.md`)
- Step 3: complete (see `STEP3_PARSER_ROBUSTNESS.md`)
- Step 4: complete (see `STEP4_BINDING_SCOPE.md`)
- Step 5: complete (see `STEP5_TYPE_SYSTEM.md`)
- Step 6: complete (see `STEP6_INTERPRETER.md`)
- Step 7: complete (see `STEP7_CODEGEN.md`)
- Step 8: complete (see `STEP8_STDLIB.md`)
- Step 9: complete (see `STEP9_CLI_UX.md`)
- Step 10: complete (see `STEP10_TEST_HARDENING.md`)
