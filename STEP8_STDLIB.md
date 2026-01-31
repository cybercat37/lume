# Step 8: Standard Library Minimal (12 Sub-steps)

Goal: introduce a minimal standard library surface for core I/O and utilities.

1) Library surface definition
   Define a small set of built-ins (e.g., `print`, `println`, `input`).
   DoD:
   - Built-ins are documented and tested.

2) Built-in symbol model
   Add a representation for built-in functions in the binder.
   DoD:
   - Binder can resolve built-in names without user declarations.

3) Print/println integration
   Integrate built-ins with interpreter and codegen.
   DoD:
   - `print`/`println` produce correct output.

4) Input stub
   Add a placeholder for input (interpreter mockable).
   DoD:
   - Interpreter can inject input for tests.

5) String helpers
   Add minimal string helpers (length, concatenation if needed).
   DoD:
   - Helpers are validated in binder/type system.

6) Math helpers
   Provide minimal math helpers (abs, min, max).
   DoD:
   - Helpers are resolved as built-ins.

7) Error messages
   Clear diagnostics for unknown built-ins or invalid calls.
   DoD:
   - Errors include function name and expected args.

8) Interpreter support
   Execute built-ins during interpretation.
   DoD:
   - Built-ins behave consistently in interpreter.

9) Codegen support
   Emit C# for built-ins (map to `Console.*` etc.).
   DoD:
   - Generated code compiles and matches semantics.

10) Testing harness
    Add helpers to test built-ins deterministically.
    DoD:
    - Tests can inject input and capture output.

11) Golden tests
    Add golden tests for built-in codegen output.
    DoD:
    - Golden output checked into repo.

12) Tests
    Add tests for built-ins and diagnostics.
    DoD:
    - Cover success and failure cases.

## Status
- Steps 1-12: not started
