using Axom.Compiler.Diagnostics;
using Axom.Compiler.Lexing;
using Axom.Compiler.Syntax;
using Axom.Compiler.Text;

namespace Axom.Compiler.Parsing;

public sealed class Parser
{
    private readonly SourceText sourceText;
    private readonly IReadOnlyList<SyntaxToken> tokens;
    private readonly List<Diagnostic> diagnostics;
    private int position;
    private static readonly HashSet<TokenKind> StatementStartTokens = new()
    {
        TokenKind.OpenBrace,
        TokenKind.LetKeyword,
        TokenKind.PrintKeyword,
        TokenKind.PrintlnKeyword,
        TokenKind.FnKeyword,
        TokenKind.ReturnKeyword,
        TokenKind.Identifier,
        TokenKind.InputKeyword,
        TokenKind.TrueKeyword,
        TokenKind.FalseKeyword,
        TokenKind.NumberLiteral,
        TokenKind.StringLiteral,
        TokenKind.OpenParen,
        TokenKind.Plus,
        TokenKind.Minus
    };

    private static readonly HashSet<TokenKind> BlockSyncTokens = new()
    {
        TokenKind.CloseBrace,
        TokenKind.NewLine,
        TokenKind.Semicolon
    };

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
                if (!IsStatementStart(Current().Kind))
                {
                    diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("statement", Current())));
                    SynchronizeStatement();
                }
                else
                {
                    var statement = ParseStatement();
                    statements.Add(statement);
                }
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
            TokenKind.PrintlnKeyword => ParsePrintStatement(),
            TokenKind.ReturnKeyword => ParseReturnStatement(),
            TokenKind.FnKeyword when Peek(1).Kind == TokenKind.Identifier => ParseFunctionDeclaration(),
            _ => ParseExpressionStatement()
        };
    }

    private StatementSyntax ParseFunctionDeclaration()
    {
        var fnKeyword = MatchToken(TokenKind.FnKeyword, "fn");
        var identifier = MatchToken(TokenKind.Identifier, "identifier");
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var parameters = ParseParameterList();
        var closeParen = MatchToken(TokenKind.CloseParen, ")");

        SyntaxToken? returnArrow = null;
        TypeSyntax? returnType = null;
        if (Current().Kind == TokenKind.ArrowType)
        {
            returnArrow = NextToken();
            returnType = ParseTypeSyntax();
        }

        if (Current().Kind == TokenKind.Arrow)
        {
            var arrowToken = NextToken();
            var expressionBody = ParseExpression();
            return new FunctionDeclarationSyntax(
                fnKeyword,
                identifier,
                openParen,
                parameters,
                closeParen,
                returnArrow,
                returnType,
                arrowToken,
                null,
                expressionBody);
        }

        var bodyBlock = ParseBlockStatement();
        return new FunctionDeclarationSyntax(
            fnKeyword,
            identifier,
            openParen,
            parameters,
            closeParen,
            returnArrow,
            returnType,
            null,
            (BlockStatementSyntax)bodyBlock,
            null);
    }

    private StatementSyntax ParseReturnStatement()
    {
        var returnKeyword = MatchToken(TokenKind.ReturnKeyword, "return");
        ExpressionSyntax? expression = null;
        if (!IsStatementTerminator(Current().Kind))
        {
            expression = ParseExpression();
        }

        if (Current().Kind == TokenKind.Semicolon)
        {
            NextToken();
        }

        return new ReturnStatementSyntax(returnKeyword, expression);
    }

    private StatementSyntax ParsePrintStatement()
    {
        var keywordToken = Current().Kind == TokenKind.PrintlnKeyword
            ? MatchToken(TokenKind.PrintlnKeyword, "println")
            : MatchToken(TokenKind.PrintKeyword, "print");
        var expression = ParseExpression();
        if (!IsStatementTerminator(Current().Kind))
        {
            diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, "Unexpected token after print statement."));
            SynchronizeStatement();
        }

        return new PrintStatementSyntax(keywordToken, expression);
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
            if (!IsStatementStart(Current().Kind))
            {
                diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("statement", Current())));
                SynchronizeBlockStatement();
            }
            else
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
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
            left = ParsePostfixExpression();
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

    private ExpressionSyntax ParsePostfixExpression()
    {
        var expression = ParsePrimaryExpression();
        while (Current().Kind == TokenKind.OpenParen)
        {
            expression = ParseCallExpression(expression);
        }

        return expression;
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
            case TokenKind.InputKeyword:
                if (Peek(1).Kind == TokenKind.OpenParen)
                {
                    return new NameExpressionSyntax(NextToken());
                }

                return new InputExpressionSyntax(NextToken());
            case TokenKind.Identifier:
                return new NameExpressionSyntax(NextToken());
            case TokenKind.FnKeyword:
                return ParseLambdaExpression();
            default:
                diagnostics.Add(Diagnostic.Error(sourceText, Current().Span, UnexpectedTokenMessage("expression", Current())));
                var missing = SyntaxToken.Missing(TokenKind.NumberLiteral, Current().Position);
                if (Current().Kind != TokenKind.EndOfFile)
                {
                    NextToken();
                }
                return new LiteralExpressionSyntax(missing);
        }
    }

    private ExpressionSyntax ParseCallExpression(ExpressionSyntax callee)
    {
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var arguments = new List<ExpressionSyntax>();
        if (Current().Kind != TokenKind.CloseParen)
        {
            do
            {
                var expression = ParseExpression();
                arguments.Add(expression);
                if (Current().Kind != TokenKind.Comma)
                {
                    break;
                }

                NextToken();
            } while (Current().Kind != TokenKind.CloseParen && Current().Kind != TokenKind.EndOfFile);
        }

        var closeParen = MatchToken(TokenKind.CloseParen, ")");
        return new CallExpressionSyntax(callee, openParen, arguments, closeParen);
    }

    private ExpressionSyntax ParseLambdaExpression()
    {
        var fnKeyword = MatchToken(TokenKind.FnKeyword, "fn");
        var openParen = MatchToken(TokenKind.OpenParen, "(");
        var parameters = ParseParameterList();
        var closeParen = MatchToken(TokenKind.CloseParen, ")");

        if (Current().Kind == TokenKind.Arrow)
        {
            var arrowToken = NextToken();
            var expressionBody = ParseExpression();
            return new LambdaExpressionSyntax(fnKeyword, openParen, parameters, closeParen, arrowToken, null, expressionBody);
        }

        var bodyBlock = ParseBlockStatement();
        return new LambdaExpressionSyntax(fnKeyword, openParen, parameters, closeParen, null, (BlockStatementSyntax)bodyBlock, null);
    }

    private List<FunctionParameterSyntax> ParseParameterList()
    {
        var parameters = new List<FunctionParameterSyntax>();
        if (Current().Kind == TokenKind.CloseParen)
        {
            return parameters;
        }

        do
        {
            var identifier = MatchToken(TokenKind.Identifier, "identifier");
            var colon = MatchToken(TokenKind.Colon, ":");
            var type = ParseTypeSyntax();
            parameters.Add(new FunctionParameterSyntax(identifier, colon, type));

            if (Current().Kind != TokenKind.Comma)
            {
                break;
            }

            NextToken();
        } while (Current().Kind != TokenKind.CloseParen && Current().Kind != TokenKind.EndOfFile);

        return parameters;
    }

    private TypeSyntax ParseTypeSyntax()
    {
        var identifier = MatchToken(TokenKind.Identifier, "type name");
        return new NameTypeSyntax(identifier);
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
            UnexpectedTokenMessage(expectedDescription, current)));

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

    private static bool IsStatementStart(TokenKind kind) =>
        StatementStartTokens.Contains(kind);

    private void SynchronizeStatement()
    {
        while (Current().Kind != TokenKind.EndOfFile && !IsStatementStart(Current().Kind))
        {
            if (Current().Kind == TokenKind.NewLine || Current().Kind == TokenKind.Semicolon)
            {
                ConsumeSeparators();
                return;
            }

            NextToken();
        }
    }

    private void SynchronizeBlockStatement()
    {
        while (Current().Kind != TokenKind.EndOfFile)
        {
            if (BlockSyncTokens.Contains(Current().Kind) || IsStatementStart(Current().Kind))
            {
                return;
            }

            NextToken();
        }
    }

    private static string UnexpectedTokenMessage(string expectedDescription, SyntaxToken actual)
    {
        var tokenText = string.IsNullOrEmpty(actual.Text) ? actual.Kind.ToString() : actual.Text;
        return $"Expected {expectedDescription}, found '{tokenText}'.";
    }

    private static bool IsStatementTerminator(TokenKind kind) =>
        kind == TokenKind.NewLine ||
        kind == TokenKind.Semicolon ||
        kind == TokenKind.EndOfFile ||
        kind == TokenKind.CloseBrace;

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
