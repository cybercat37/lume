# Step 3: Parser Robustness (12 Sub-steps)

Goal: improve parser resilience with recovery, clearer diagnostics, and consistent behavior on malformed input.

1) Recovery strategy definition
   Define synchronization tokens per context (top-level, block, expression).
   DoD:
   - Document sync token sets in code.
   - Parser can skip to sync points without infinite loops.

2) Token classification helpers
   Add helpers for token kinds (statement starters, expression starters).
   DoD:
   - Centralized helpers used by parser decisions.

3) Missing token diagnostics
   Improve messages for missing tokens (expected X, found Y).
   DoD:
   - Diagnostics include expected token description.

4) Unexpected token recovery
   Skip unexpected tokens and continue parsing statements.
   DoD:
   - Parser continues after junk input and emits diagnostics.

5) Block recovery
   Recover inside blocks when `}` is missing or extra tokens appear.
   DoD:
   - Blocks terminate on sync tokens or EOF with diagnostics.

6) Expression recovery
   Recover from invalid operator/operand sequences.
   DoD:
   - Expression parser returns a node and diagnostic instead of failing.

7) Assignment recovery
   Handle `name =` without RHS with a diagnostic and placeholder.
   DoD:
   - Assignment nodes created with missing RHS token.

8) Parenthesis recovery
   Handle missing `)` and nested parentheses gracefully.
   DoD:
   - Missing `)` yields a diagnostic and continues parsing.

9) Newline/semicolon recovery
   Allow extra separators without failing the parse.
   DoD:
   - Multiple separators are consumed cleanly.

10) Diagnostic formatting
    Standardize diagnostic messages and include token text when helpful.
    DoD:
    - Message templates are consistent across parser errors.

11) Failing input still yields syntax tree
    Ensure `SyntaxTree` always returns a root with diagnostics.
    DoD:
    - Parser never throws on invalid input in normal flow.

12) Tests
    Add tests covering recovery scenarios and diagnostics.
    DoD:
    - Tests verify parsing continues after errors.

## Status
- Steps 1-12: complete
