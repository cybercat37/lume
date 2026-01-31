# Step 5: Type System Foundation (12 Sub-steps)

Goal: introduce primitive types, basic type checking, and early inference.

1) Type model
   Define core types (Int, Bool, String, Error) and a base type symbol.
   DoD:
   - Types are immutable and comparable.

2) Literal typing
   Bind literals with concrete types.
   DoD:
   - Literal expressions carry a type in bound nodes.

3) Variable typing
   Track variable types on declaration from initializer.
   DoD:
   - Variables store a type symbol.

4) Assignment type checking
   Ensure assigned expression type matches variable type.
   DoD:
   - Mismatches yield diagnostics.

5) Unary operator typing
   Validate unary operators by operand type.
   DoD:
   - `-` only for Int, `+` only for Int.

6) Binary operator typing
   Validate binary operators by operand types.
   DoD:
   - `+ - * /` only for Int; add Bool comparisons later.

7) Name expression typing
   Bound name expressions expose the symbol type.
   DoD:
   - Type flows through bound expressions.

8) Print typing
   Allow print for any primitive type.
   DoD:
   - No diagnostics for printing Int/Bool/String.

9) Error type propagation
   When a node has errors, propagate an Error type.
   DoD:
   - Further checks avoid cascading diagnostics.

10) Diagnostic messages
    Standardize type mismatch diagnostics.
    DoD:
    - Messages include expected/actual types.

11) Binder integration
    Extend binder to attach types to bound nodes.
    DoD:
    - Bound nodes expose a Type property.

12) Tests
    Add tests for type inference and mismatch errors.
    DoD:
    - Cover literals, assignments, unary/binary ops.

## Status
- Steps 1-12: complete
