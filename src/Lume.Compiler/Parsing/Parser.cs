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
        ConsumeSeparators();
        var statements = new List<StatementSyntax>();
        if (Current().Kind == TokenKind.EndOfFile)
        {
            diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, "Expected statement."));
        }
        else
        {
            while (Current().Kind != TokenKind.EndOfFile)
            {
                var start = position;
                var statement = ParseStatement();
                statements.Add(statement);
                ConsumeSeparators();

                if (position == start)
                {
                    NextToken();
                }
            }
        }

        var endOfFileToken = MatchToken(TokenKind.EndOfFile, "end of file");
        return new CompilationUnitSyntax(statements, endOfFileToken);
    }

    private StatementSyntax ParseStatement()
    {
        return Current().Kind switch
        {
            TokenKind.OpenBrace => ParseBlockStatement(),
            TokenKind.LetKeyword => ParseVariableDeclaration(),
            TokenKind.PrintKeyword => ParsePrintStatement(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementSyntax ParsePrintStatement()
    {
        var printKeyword = MatchToken(TokenKind.PrintKeyword, "print");
        var expression = ParseExpression();
        return new PrintStatementSyntax(printKeyword, expression);
    }

    private StatementSyntax ParseVariableDeclaration()
    {
        var letKeyword = MatchToken(TokenKind.LetKeyword, "let");
        SyntaxToken? mutKeyword = null;
        if (Current().Kind == TokenKind.MutKeyword)
        {
            mutKeyword = NextToken();
        }

        var identifier = MatchToken(TokenKind.Identifier, "identifier");
        var equalsToken = MatchToken(TokenKind.EqualsToken, "=");
        var initializer = ParseExpression();
        SyntaxToken? semicolonToken = null;
        if (Current().Kind == TokenKind.Semicolon)
        {
            semicolonToken = NextToken();
        }

        return new VariableDeclarationSyntax(
            letKeyword,
            mutKeyword,
            identifier,
            equalsToken,
            initializer,
            semicolonToken);
    }

    private StatementSyntax ParseExpressionStatement()
    {
        var expression = ParseExpression();
        if (Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }
        return new ExpressionStatementSyntax(expression);
    }

    private StatementSyntax ParseBlockStatement()
    {
        var openBrace = MatchToken(TokenKind.OpenBrace, "{");
        var statements = new List<StatementSyntax>();

        ConsumeSeparators();
        while (Current().Kind != TokenKind.CloseBrace && Current().Kind != TokenKind.EndOfFile)
        {
            var start = position;
            var statement = ParseStatement();
            statements.Add(statement);
            ConsumeSeparators();

            if (position == start)
            {
                NextToken();
            }
        }

        var closeBrace = MatchToken(TokenKind.CloseBrace, "}");
        return new BlockStatementSyntax(openBrace, statements, closeBrace);
    }

    private ExpressionSyntax ParseExpression() =>
        ParseAssignmentExpression();

    private ExpressionSyntax ParseAssignmentExpression()
    {
        if (Current().Kind == TokenKind.Identifier && Peek(1).Kind == TokenKind.EqualsToken)
        {
            var identifier = NextToken();
            var equalsToken = NextToken();
            var right = ParseAssignmentExpression();
            return new AssignmentExpressionSyntax(identifier, equalsToken, right);
        }

        return ParseBinaryExpression();
    }

    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryOperatorPrecedence(Current().Kind);
        if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(Current().Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current().Kind)
        {
            case TokenKind.OpenParen:
                var openParen = NextToken();
                var expression = ParseExpression();
                var closeParen = MatchToken(TokenKind.CloseParen, ")");
                return new ParenthesizedExpressionSyntax(openParen, expression, closeParen);
            case TokenKind.TrueKeyword:
            case TokenKind.FalseKeyword:
            case TokenKind.NumberLiteral:
            case TokenKind.StringLiteral:
                return new LiteralExpressionSyntax(NextToken());
            case TokenKind.Identifier:
                return new NameExpressionSyntax(NextToken());
            default:
                var missing = MatchToken(TokenKind.NumberLiteral, "expression");
                return new LiteralExpressionSyntax(missing);
        }
    }

    private void ConsumeSeparators()
    {
        while (Current().Kind == TokenKind.NewLine || Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }
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

    private static int GetBinaryOperatorPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Star => 2,
            TokenKind.Slash => 2,
            TokenKind.Plus => 1,
            TokenKind.Minus => 1,
            _ => 0
        };
    }

    private static int GetUnaryOperatorPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Plus => 3,
            TokenKind.Minus => 3,
            _ => 0
        };
    }
}
