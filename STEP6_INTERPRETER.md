# Step 6: Interpreter Runtime (12 Sub-steps)

Goal: execute bound programs directly for fast feedback and validation.

1) Interpreter entry point
   Create an interpreter that runs a bound program and returns output/diagnostics.
   DoD:
   - `Interpreter.Run` produces output and diagnostics.

2) Output capture
   Collect printed output deterministically.
   DoD:
   - Output preserves statement order.

3) Literal evaluation
   Evaluate Int/Bool/String literals.
   DoD:
   - Literal values round-trip correctly.

4) Variable storage
   Track variable values by symbol.
   DoD:
   - Declarations initialize values.

5) Assignment evaluation
   Update mutable variables.
   DoD:
   - Assignments reflect in later reads.

6) Block execution
   Execute statements inside blocks in order.
   DoD:
   - Inner blocks run without affecting outer scope binding rules.

7) Unary evaluation
   Support unary + and - on Int.
   DoD:
   - Unary expressions evaluate correctly.

8) Binary evaluation
   Support +, -, *, / on Int with precedence handled by parser.
   DoD:
   - Binary expressions evaluate correctly.

9) Expression statements
   Evaluate expressions without output.
   DoD:
   - Expression statements produce no output.

10) Error handling
   Stop execution on runtime errors (e.g., divide by zero).
   DoD:
   - Diagnostics returned and execution halts.

11) Shadowing behavior
   Ensure reads and writes respect bound symbols in nested scopes.
   DoD:
   - Inner scope updates affect the correct symbol.

12) Tests
   Add interpreter tests for literals, variables, blocks, and errors.
   DoD:
   - Tests cover success paths and runtime errors.

## Status
- Steps 1-12: complete
