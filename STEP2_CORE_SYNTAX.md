# Step 2: Core Syntax Expansion (12 Sub-steps)

Goal: add core language constructs (numbers, variables, assignments, blocks, and basic expressions) with diagnostics and tests.

1) Token kinds for core syntax
   Add token kinds for numbers, operators, separators, and keywords (`let`, `var`, `true`, `false`).
   DoD:
   - Token enum includes number literals and punctuation.
   - Keywords are distinguished from identifiers.

2) Lexer numeric literals
   Parse integer literals (base 10) with overflow diagnostics.
   DoD:
   - `123` lexes as a number token with value `123`.
   - Overflows produce diagnostics and still return a token.

3) Lexer punctuation
   Support `=`, `+`, `-`, `*`, `/`, `(`, `)`, `{`, `}`, `;`.
   DoD:
   - Each punctuation has a dedicated token kind.
   - Unexpected characters yield diagnostics.

4) Expression AST expansion
   Add `BinaryExpressionSyntax`, `UnaryExpressionSyntax`, `ParenthesizedExpressionSyntax`.
   DoD:
   - Nodes carry operator tokens and child expressions.

5) Literal expression expansion
   Support numeric and boolean literals in `LiteralExpressionSyntax`.
   DoD:
   - Literal nodes preserve the token value type.

6) Variable declaration syntax
   Introduce `VariableDeclarationSyntax` (`let` or `var` name `=` expression `;`).
   DoD:
   - AST node stores keyword, identifier, equals, expression, semicolon.

7) Assignment expression
   Add `AssignmentExpressionSyntax` (`name = expression`).
   DoD:
   - Parser distinguishes assignments from binary expressions.

8) Block statement
   Add `BlockStatementSyntax` for `{ ... }` with statement lists.
   DoD:
   - Parser collects statements until `}` or EOF.

9) Statement list infrastructure
   Introduce a `SeparatedSyntaxList` or simple list for statements.
   DoD:
   - `CompilationUnitSyntax` can hold multiple statements.

10) Parser expression precedence
    Implement precedence climbing for unary and binary operators.
    DoD:
    - `1 + 2 * 3` parses with correct precedence.

11) Diagnostics for core syntax
    Add parser errors for missing tokens and invalid constructs.
    DoD:
    - Missing `;` or `}` yields targeted diagnostics.

12) Tests
    Add tests for lexing, parsing, and errors across new syntax.
    DoD:
    - Success and failure cases covered for literals, assignments, and blocks.

## Status
- Steps 1-12: not started
