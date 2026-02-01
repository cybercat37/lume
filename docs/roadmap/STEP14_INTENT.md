# Step 14: Intent Annotations (v0.4)

Goal: introduce `@intent("...")` as a built-in attribute for semantic documentation and diagnostics.

1) Lexer + tokens
   Add tokens for `@` and intent identifiers.
   DoD:
   - `@intent("...")` lexes without diagnostics.

2) Parser + AST
   Parse intent annotations on blocks and `let` bindings.
   DoD:
   - AST contains intent nodes for block/let.
   - Syntax errors produce diagnostics.

3) Binder metadata
   Attach intent metadata to bound nodes.
   DoD:
   - Bound nodes carry intent strings.
   - Intent does not affect runtime semantics.

4) Effect inference (stub)
   Infer basic effects from known operations (db/network/fs/time/random).
   DoD:
   - Effect set recorded per node (even if minimal).

5) Diagnostics
   Emit warnings when intent mismatches inferred effects.
   DoD:
   - Warning format defined and snapshot tested.

6) Documentation output
   Provide a basic docs output path for intent summaries.
   DoD:
   - CLI or tooling can emit intent summaries.

7) Tests
   Add parser, binder, and diagnostics tests.
   DoD:
   - Tests cover block/let intents and mismatch warnings.

8) Documentation
   Update README/spec/tutorial.
   DoD:
   - Doc examples compile.

## Status
- Sub-step 1: pending
