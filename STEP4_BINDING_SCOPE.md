# Step 4: Binding and Scope (12 Sub-steps)

Goal: resolve symbols, enforce scope rules, and report semantic diagnostics.

1) Binder entry point
   Introduce a binder that consumes the syntax tree and produces a bound tree.
   DoD:
   - Binder runs after parsing and returns diagnostics.

2) Symbol model
   Define symbols for variables (name, mutability, type placeholder).
   DoD:
   - Symbols are immutable and tracked per scope.

3) Scope stack
   Implement nested scopes with parent lookup.
   DoD:
   - Lookups resolve to nearest symbol in scope chain.

4) Variable declaration binding
   Bind `let` and `let mut` into the current scope.
   DoD:
   - Redeclaration in same scope reports diagnostics.

5) Name expression binding
   Resolve identifiers to symbols.
   DoD:
   - Undefined names produce diagnostics with spans.

6) Assignment binding
   Validate assignments target mutable variables only.
   DoD:
   - Assigning to immutable variables produces diagnostics.

7) Block scope binding
   New scope per block statement.
   DoD:
   - Shadowing in inner scopes is allowed.

8) Print statement binding
   Bind print expression and ensure it is valid.
   DoD:
   - Print of invalid expression yields diagnostics.

9) Bound tree model
   Define bound nodes for statements/expressions (minimal set).
   DoD:
   - Bound tree mirrors syntax tree structure.

10) Diagnostic aggregation
    Merge parser and binder diagnostics in `CompilationResult`.
    DoD:
    - Errors from both phases are visible to the CLI.

11) Binder tests
    Tests for undefined variables, redeclarations, and mutability.
    DoD:
    - Tests cover success and failure cases.

12) Integration wiring
    Hook binder into the compiler pipeline.
    DoD:
    - Compilation fails on semantic errors.

## Status
- Steps 1-12: not started
