# Step 7: Code Generation v1 (12 Sub-steps)

Goal: generate robust C# output from the bound program with correct semantics.

1) Codegen entry point
   Use `BoundProgram` as the input for code generation.
   DoD:
   - No codegen path uses raw syntax nodes.

2) Statement coverage
   Emit code for print, variable declarations, assignments, blocks.
   DoD:
   - All bound statement kinds are handled.

3) Expression coverage
   Emit literals, names, unary, binary, assignments.
   DoD:
   - All bound expression kinds are handled.

4) Correct grouping
   Preserve operator precedence and parentheses.
   DoD:
   - Parenthesized expressions emit grouping.

5) String escaping
   Escape special characters consistently (`\n`, `\t`, `\r`, quotes, backslashes).
   DoD:
   - Emitted string literals round-trip.

6) Indentation and formatting
   Produce readable, deterministic formatting.
   DoD:
   - Indentation is consistent in nested blocks.

7) Deterministic output
   Same input yields byte-identical output.
   DoD:
   - Golden test can compare output files.

8) Error handling
   Fail codegen when bound program has errors.
   DoD:
   - Codegen is not invoked on diagnostics.

9) Minimal runtime assumptions
   Output compiles under .NET 8 without extra dependencies.
   DoD:
   - Generated code uses only `System` and standard library.

10) CLI integration
    Ensure CLI uses new codegen path for build.
    DoD:
    - `axom build` uses codegen output.

11) Golden tests
    Add golden tests for codegen output.
    DoD:
    - Expected output checked into repo.

12) Tests
    Add unit tests for codegen edge cases.
    DoD:
    - Covers grouping, escaping, and blocks.

## Status
- Steps 1-12: complete
