# Step 13: Functions and Lambdas (v0.3)

Goal: introduce functions and lambdas with basic type inference and codegen/interpreter support.

1) Function syntax
   Add `fn name(params) { ... }` and `fn name(params) => expr`.
   DoD:
   - Parser recognizes function declarations and expression bodies.

2) Parameters and scope
   Bind parameters as locals; respect shadowing and mutability rules.
   DoD:
   - Parameters are available in function body.

3) Return behavior
   Implicit return from last expression; explicit `return` allowed.
   DoD:
   - `return` supported in parser/binder.

4) Return type inference
   Infer return type from body when no annotation.
   DoD:
   - Diagnostics for inconsistent returns.

5) Call expressions
   Resolve calls by name and arity.
   DoD:
   - Errors on unknown functions or wrong arity.

6) First-class functions
   Allow functions as values.
   DoD:
   - Function types introduced in binder.

7) Lambda single-expression
   Support `fn(x) => expr` as expression.
   DoD:
   - Parses and binds as lambda.

8) Lambda block body
   Support `fn(x) { ... }` with implicit return.
   DoD:
   - Return inference applies to block.

9) Closures
   Capture outer variables by value.
   DoD:
   - Captured values available in lambda.

10) Semantics errors
   Diagnostics for return/type mismatch, invalid captures, etc.
   DoD:
   - Snapshot tests for function errors.

11) Runtime + codegen
   Implement interpreter and codegen support for functions/lambdas.
   DoD:
   - End-to-end tests for function calls.

12) Documentation
   Update README/spec/tutorial with function syntax and examples.
   DoD:
   - Doc examples compile.

## Status
- Sub-step 1: pending
