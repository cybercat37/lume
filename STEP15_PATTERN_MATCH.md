# Step 15: Pattern Match v1 (v0.5)

Goal: implement `match` as the primary branching construct with
exhaustiveness and strong diagnostics.

## Sub-steps

1) Lexer and tokens
   Ensure tokens for `match`, `->`, and pattern punctuation are stable.
   DoD:
   - `match` and `->` tokenize without diagnostics.
   - Token spans are correct for error reporting.

2) Parser and syntax tree
   Parse match expressions with multiple arms.
   DoD:
   - Grammar supports: `match expr { pattern -> expr ... }`.
   - Allows trailing commas or newlines between arms.
   - Syntax errors produce diagnostics but still return a tree.

3) Pattern AST shapes
   Add pattern node kinds for literals, wildcards, identifiers, and tuples.
   DoD:
   - Patterns have source spans for diagnostics.
   - Tuple patterns align with tuple literal arity.

4) Binding and pattern variables
   Bind identifiers introduced by patterns into a new arm scope.
   DoD:
   - Duplicate bindings in the same pattern are diagnosed.
   - Pattern variables are visible only within their arm expression.

5) Type checking
   Validate pattern compatibility and arm expression types.
   DoD:
   - Pattern types are checked against the matched expression type.
   - Arm expression types are unified or produce a diagnostic.
   - `match` expression has a final resolved type.

6) Exhaustiveness and unreachable diagnostics
   Enforce exhaustive matching for supported pattern sets.
   DoD:
   - Missing cases produce a diagnostic with a summary of uncovered patterns.
   - Unreachable arms are reported.
   - Literal and wildcard cases are supported for v1.

7) Interpreter semantics
   Execute `match` with correct short-circuiting.
   DoD:
   - First matching arm is evaluated; others are skipped.
   - Bound pattern variables evaluate to matched values.

8) Code generation
   Emit valid C# for `match` expressions.
   DoD:
   - Generated code preserves arm order and semantics.
   - Diagnostics for unsupported patterns are surfaced before emit.

9) Tests
   Add parser, binder, and execution tests.
   DoD:
   - Tests cover literals, wildcards, tuples, and identifier binding.
   - Exhaustiveness and unreachable diagnostics are snapshot tested.

10) Documentation
   Update tutorial/spec to reflect implemented `match` behavior.
   DoD:
   - Examples compile and run.
   - Any limitations are documented.

## Status
- Sub-step 1: pending
