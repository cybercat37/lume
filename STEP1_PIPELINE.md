# Step 1: Pipeline Base (12 Sub-steps)

Goal: establish a robust compile pipeline that can parse and emit a minimal program with reliable diagnostics.

1) Source text and spans
   Introduce `SourceText` and `TextSpan` for positioning and diagnostics.
   DoD:
   - `SourceText` maps positions to line/column.
   - `TextSpan` supports start/length and bounds.

2) Diagnostic infrastructure
   Enrich `Diagnostic` with spans and consistent error formatting.
   DoD:
   - Diagnostics carry spans and file/line/column.
   - Factory methods accept `SourceText` + `TextSpan`.

3) Token model
   Define `TokenKind` and `SyntaxToken` for lexing output.
   DoD:
   - Tokens expose kind, span, text, and value.
   - Missing tokens are representable.

4) Lexer minimal
   Support identifiers, `print`, string literals, EOF, and bad tokens.
   DoD:
   - Lexer returns `print` keyword and string literals.
   - Unterminated strings emit diagnostics.

5) AST base
   Create `SyntaxNode` plus `StatementSyntax` and `ExpressionSyntax`.
   DoD:
   - Base nodes include spans and file-scoped namespaces.

6) Print statement AST
   Add `PrintStatementSyntax` and `LiteralExpressionSyntax` nodes.
   DoD:
   - `print` statement carries keyword + expression.
   - Literal expression carries token value.

7) Parser minimal
   Parse `print "string"` and produce a `CompilationUnitSyntax` root.
   DoD:
   - Parser emits errors for unexpected tokens.
   - Syntax tree is produced even with errors.

8) Syntax tree
   Build `SyntaxTree` that aggregates lexer+parser diagnostics.
   DoD:
   - Diagnostics list includes both lexer and parser errors.

9) Emitter minimal
   Emit `Console.WriteLine` from the AST.
   DoD:
   - Emitted code prints the literal value.

10) Compiler pipeline integration
    Wire lexer/parser/emitter in `CompilerDriver`.
    DoD:
    - Pipeline returns diagnostics on failures.
    - Success returns generated code and syntax tree.

11) CLI compatibility
    Ensure CLI behavior and error handling remain stable.
    DoD:
    - CLI usage and exit codes are unchanged.

12) Tests
    Add lexer, parser, and pipeline tests for success and error flows.
    DoD:
    - Tests cover success + error cases for each phase.

## Status
- Steps 1-12: in progress

## Definition of Done
- A minimal program `print "hello"` compiles and runs through the CLI.
- Unterminated strings and unexpected characters produce diagnostics with spans.
- Lexer, parser, and pipeline tests pass in `dotnet test`.
- No user-facing regressions in CLI usage or exit codes.
