using Lume.Compiler.Diagnostics;
using Lume.Compiler.Lexing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

namespace Lume.Compiler.Parsing;

public sealed class Parser
{
    private readonly SourceText sourceText;
    private readonly IReadOnlyList<SyntaxToken> tokens;
    private readonly List<Diagnostic> diagnostics;
    private int position;

    public Parser(SourceText sourceText, IReadOnlyList<SyntaxToken> tokens)
    {
        this.sourceText = sourceText;
        this.tokens = tokens;
        diagnostics = new List<Diagnostic>();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var statement = ParseStatement();
        var endOfFileToken = MatchToken(TokenKind.EndOfFile, "end of file");
        return new CompilationUnitSyntax(statement, endOfFileToken);
    }

    private StatementSyntax ParseStatement()
    {
        var printKeyword = MatchToken(TokenKind.PrintKeyword, "print");
        var expression = ParseExpression();
        return new PrintStatementSyntax(printKeyword, expression);
    }

    private ExpressionSyntax ParseExpression()
    {
        var literalToken = MatchToken(TokenKind.StringLiteral, "string literal");
        return new LiteralExpressionSyntax(literalToken);
    }

    private SyntaxToken MatchToken(TokenKind kind, string expectedDescription)
    {
        if (Current().Kind == kind)
        {
            return NextToken();
        }

        var current = Current();
        diagnostics.Add(Diagnostic.Error(
            sourceText,
            current.Span,
            $"Expected {expectedDescription}."));

        return SyntaxToken.Missing(kind, current.Position);
    }

    private SyntaxToken Current() => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = position + offset;
        if (index >= tokens.Count)
        {
            return tokens[^1];
        }

        return tokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = Current();
        position++;
        return current;
    }
}
